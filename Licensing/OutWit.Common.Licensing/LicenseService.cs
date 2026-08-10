using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Licensing.Abstract;
using OutWit.Common.Licensing.Crypto;
using OutWit.Common.Licensing.Demo;
using OutWit.Common.Licensing.Fingerprint;
using OutWit.Common.Licensing.Requests;
using OutWit.Common.Licensing.Snapshot;
using OutWit.Common.Licensing.Validation;

namespace OutWit.Common.Licensing
{
    /// <summary>
    /// Evaluates the installed licences and exposes the result.
    /// </summary>
    public sealed class LicenseService : ILicenseService, IDisposable
    {
        #region Constants

        /// <summary>
        /// Which refusal to report when several licences are installed and none
        /// is valid — most actionable first. "Expired" sends a customer to a
        /// renewal; "malformed" sends them nowhere, so it must never mask the
        /// former.
        /// </summary>
        private static readonly LicenseStatus[] FAILURE_PRIORITY =
        {
            LicenseStatus.Expired,
            LicenseStatus.NotYetValid,
            LicenseStatus.BindingMismatch,
            LicenseStatus.WrongVersion,
            LicenseStatus.ExceedsKeyPolicy,
            LicenseStatus.WrongProduct,
            LicenseStatus.SignatureInvalid,
            LicenseStatus.UnknownKeyId,
            LicenseStatus.Malformed
        };

        #endregion

        #region Events

        public event LicenseStateEventHandler? StateChanged;

        #endregion

        #region Fields

        private readonly LicensingOptions m_options;
        private readonly LicenseValidator m_validator;
        private readonly SemaphoreSlim m_reloadLock = new(1, 1);

        private IReadOnlyList<LicenseFactor> m_factors = Array.Empty<LicenseFactor>();

        private Timer? m_timer;
        private bool m_disposed;

        #endregion

        #region Constructors

        public LicenseService(LicensingOptions options)
        {
            m_options = options;
            m_validator = new LicenseValidator(options.KeyRing, options.Product, options.ProductVersion);

            State = new LicenseState(
                LicenseValidationResult.Failure(LicenseStatus.Missing),
                isDemo: false,
                fingerprint: string.Empty,
                unrecognisedKeys: Array.Empty<string>(),
                evaluatedUtc: options.Clock(),
                grace: options.Grace);

            Snapshot = LicenseSnapshot.Empty();

            InitTimer();
        }

        #endregion

        #region Initialization

        private void InitTimer()
        {
            if (m_options.ReloadInterval == null)
                return;

            var interval = m_options.ReloadInterval.Value;

            m_timer = new Timer(OnTimer, null, interval, interval);
        }

        #endregion

        #region ILicenseService

        public LicenseState State { get; private set; }

        public LicenseSnapshot Snapshot { get; private set; }

        public string Fingerprint => State.Fingerprint;

        public bool HasFeature(string key)
        {
            return State.CanRun && State.Payload?.HasFeature(key) == true;
        }

        public long Limit(string key, long fallback = long.MaxValue)
        {
            var declared = m_options.Vocabulary.DefaultFor(key, fallback);

            if (!State.CanRun || State.Payload == null)
                return declared;

            return State.Payload.Limit(key, declared);
        }

        public async Task ReloadAsync()
        {
            await m_reloadLock.WaitAsync().ConfigureAwait(false);

            try
            {
                m_factors = await m_options.BindingProvider.CollectAsync().ConfigureAwait(false);

                var fingerprint = FingerprintCodec.Encode(m_options.FingerprintPrefix, m_factors);
                var utcNow = m_options.Clock();

                var stored = m_options.Store.ReadState();

                // The clock is now a modifier on the state rather than a
                // replacement for it: the licences are still read and still
                // reported, so a panel can say "your licence is fine, this
                // machine's clock reads 2019" instead of only "clock tampered"
                // to a customer whose CMOS battery died.
                var isClockSuspect = m_options.ClockGuard.IsTampered(stored, utcNow);
                var clockBehindBy = isClockSuspect ? stored.HighWaterMarkUtc - utcNow : (TimeSpan?)null;

                // Never advanced while the clock is not trusted: seeding the
                // first-run stamp from a wound-back clock would anchor the demo
                // term to a date that never happened.
                if (!isClockSuspect)
                    m_options.Store.WriteState(m_options.ClockGuard.Observe(stored, utcNow));

                var firstRunUtc = stored.FirstRunUtc == default ? utcNow : stored.FirstRunUtc;

                Apply(Evaluate(fingerprint, firstRunUtc, utcNow, isClockSuspect, clockBehindBy));
            }
            finally
            {
                m_reloadLock.Release();
            }
        }

        public async Task<LicenseValidationResult> InstallAsync(string token)
        {
            var result = m_validator.Validate(token, m_factors, m_options.Clock());

            // A licence installed before its start date is legitimate — that is
            // how a renewal is staged ahead of time — so NotYetValid is stored
            // too. Everything else that fails is refused rather than allowed to
            // displace a working licence.
            if (result.Status is LicenseStatus.Valid or LicenseStatus.NotYetValid)
            {
                m_options.Store.Save(token);
                await ReloadAsync().ConfigureAwait(false);
            }

            return result;
        }

        public async Task<bool> RemoveAsync(string licenseId)
        {
            if (string.IsNullOrWhiteSpace(licenseId))
                return false;

            if (!m_options.Store.Remove(licenseId))
                return false;

            await ReloadAsync().ConfigureAwait(false);

            return true;
        }

        public async Task<LicenseRequest> CreateRequestAsync(string? host = null, string? contact = null, string? notes = null)
        {
            var factors = await m_options.BindingProvider.CollectAsync().ConfigureAwait(false);

            return new LicenseRequest
            {
                Product = m_options.Product,
                ProductVersion = m_options.ProductVersion?.ToString() ?? string.Empty,
                Fingerprint = FingerprintCodec.Encode(m_options.FingerprintPrefix, factors),
                Factors = factors,
                Host = host ?? DescribeHost(),
                Contact = contact,
                Notes = notes
            };
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (m_disposed)
                return;

            m_disposed = true;

            m_timer?.Dispose();
            m_timer = null;

            m_reloadLock.Dispose();
        }

        #endregion

        #region Tools

        private Evaluation Evaluate(
            string fingerprint,
            DateTime firstRunUtc,
            DateTime utcNow,
            bool isClockSuspect,
            TimeSpan? clockBehindBy)
        {
            // Parsed once and carried: the panel wants the signing key id per
            // document, and re-parsing every token to recover it would decode
            // and deserialise the whole store a second time on every reload.
            var documents = m_options.Store
                .ReadTokens()
                .Select(LicenseToken.Parse)
                .Select(parsed => new Document(parsed, Validate(parsed, utcNow)))
                .ToList();

            var valid = documents.Where(document => document.Result.IsValid).ToList();

            // Supersession is honoured only from licences that are themselves
            // valid right now. A document cannot retire another one before its
            // own start date, and a forged one cannot retire anything at all.
            var superseded = new HashSet<string>(
                valid.SelectMany(document => document.Result.Payload!.Supersedes),
                StringComparer.OrdinalIgnoreCase);

            var effective = valid
                .Where(document => !superseded.Contains(document.Result.Payload!.Id))
                .OrderByDescending(document => document.Result.Payload!.IsUnlimited)
                .ThenByDescending(document => document.Result.Payload!.ExpiresUtc ?? DateTime.MaxValue)
                .ThenByDescending(document => document.Result.Payload!.IssuedUtc)
                .FirstOrDefault();

            var results = documents.Select(document => document.Result).ToList();

            var (result, isDemo, demoExpiresUtc) = Select(effective, results, firstRunUtc, utcNow);

            var state = new LicenseState(
                Wrap(result, isClockSuspect),
                isDemo,
                fingerprint,
                Unrecognised(result.Payload),
                utcNow,
                m_options.Grace,
                demoExpiresUtc,
                isClockSuspect,
                clockBehindBy);

            var installed = documents
                .Select(document => LicenseSnapshotFactory.Describe(
                    document.Token,
                    document.Result,
                    isEffective: ReferenceEquals(document, effective)))
                .ToList();

            return new Evaluation(state, installed);
        }

        /// <summary>Picks what the state is built from: the licence in force, a demo, or the best refusal.</summary>
        private (LicenseValidationResult Result, bool IsDemo, DateTime? DemoExpiresUtc) Select(
            Document? effective,
            IReadOnlyList<LicenseValidationResult> results,
            DateTime firstRunUtc,
            DateTime utcNow)
        {
            if (effective != null)
                return (effective.Result, false, null);

            // Demo applies only to a host that has never had a licence at all.
            // Once one has been installed, that licence's own refusal is the
            // actionable answer: telling a customer whose purchase lapsed that
            // "the demo period has ended" hides the expiry date, the customer
            // name and the renewal they actually need.
            if (results.Count == 0 && m_options.Demo != null)
            {
                var payload = DemoLicenseFactory.Create(m_options.Product, m_options.Demo, firstRunUtc);

                var result = payload.ExpiresUtc > utcNow
                    ? LicenseValidationResult.Valid(payload)
                    : LicenseValidationResult.Failure(LicenseStatus.Expired, payload);

                return (result, true, payload.ExpiresUtc);
            }

            return (BestFailure(results), false, null);
        }

        /// <summary>
        /// Reports an untrusted clock as the verdict while keeping the payload,
        /// so <see cref="LicenseState.Status"/> stays a refusal for callers that
        /// only read it — an older consumer must not start running because the
        /// state learned to describe itself better.
        /// </summary>
        private static LicenseValidationResult Wrap(LicenseValidationResult result, bool isClockSuspect)
        {
            return isClockSuspect
                ? LicenseValidationResult.Failure(LicenseStatus.ClockTampered, result.Payload, result.Detail)
                : result;
        }

        private LicenseValidationResult Validate(LicenseToken? token, DateTime utcNow)
        {
            return token == null
                ? LicenseValidationResult.Failure(LicenseStatus.Malformed)
                : m_validator.ValidateToken(token, m_factors, utcNow);
        }

        private void Apply(Evaluation evaluation)
        {
            var previous = State;

            State = evaluation.State;
            Snapshot = LicenseSnapshotFactory.Create(
                m_options, evaluation.State, evaluation.Installed, HasFeature, key => Limit(key));

            if (HasChanged(previous, evaluation.State))
                StateChanged?.Invoke(this, evaluation.State);
        }

        /// <summary>
        /// Whether anything a consumer could act on has moved. The day count is
        /// part of it: over a session that runs for weeks, "12 days remaining"
        /// going stale is the failure a periodic reload exists to prevent.
        /// </summary>
        private static bool HasChanged(LicenseState previous, LicenseState current)
        {
            return previous.Mode != current.Mode
                   || previous.Status != current.Status
                   || previous.IsClockSuspect != current.IsClockSuspect
                   || previous.DaysRemaining != current.DaysRemaining
                   || previous.ExpiresUtc != current.ExpiresUtc
                   || !string.Equals(previous.Payload?.Id, current.Payload?.Id, StringComparison.Ordinal)
                   || !string.Equals(previous.Fingerprint, current.Fingerprint, StringComparison.Ordinal);
        }

        private IReadOnlyList<string> Unrecognised(LicensePayload? payload)
        {
            if (payload == null)
                return Array.Empty<string>();

            return m_options.Vocabulary.Unrecognised(payload.Features, payload.Limits.Keys);
        }

        private static LicenseValidationResult BestFailure(IReadOnlyList<LicenseValidationResult> results)
        {
            if (results.Count == 0)
                return LicenseValidationResult.Failure(LicenseStatus.Missing);

            foreach (var status in FAILURE_PRIORITY)
            {
                var match = results.FirstOrDefault(result => result.Status == status);
                if (match != null)
                    return match;
            }

            return results[0];
        }

        private static string DescribeHost()
        {
            try
            {
                return $"{Environment.MachineName} ({Environment.OSVersion.VersionString})";
            }
            catch
            {
                return string.Empty;
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// A background re-evaluation must never be able to take the host down.
        /// Whatever went wrong — an unreadable store, a mount that disappeared —
        /// the previous state stands and the next tick tries again.
        /// </summary>
        private async void OnTimer(object? sender)
        {
            try
            {
                await ReloadAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
            }
        }

        #endregion

        #region Properties

        /// <summary>The options this service was built from.</summary>
        public LicensingOptions Options => m_options;

        #endregion

        #region Definitions

        /// <summary>One stored token, its parsed form and its verdict.</summary>
        private sealed class Document
        {
            public Document(LicenseToken? token, LicenseValidationResult result)
            {
                Token = token;
                Result = result;
            }

            public LicenseToken? Token { get; }

            public LicenseValidationResult Result { get; }
        }

        /// <summary>The outcome of one evaluation: the state, and what it was computed from.</summary>
        private sealed class Evaluation
        {
            public Evaluation(LicenseState state, IReadOnlyList<LicenseDocument> installed)
            {
                State = state;
                Installed = installed;
            }

            public LicenseState State { get; }

            public IReadOnlyList<LicenseDocument> Installed { get; }
        }

        #endregion
    }
}
