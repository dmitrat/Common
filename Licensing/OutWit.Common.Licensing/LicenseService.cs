using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OutWit.Common.Licensing.Abstract;
using OutWit.Common.Licensing.Demo;
using OutWit.Common.Licensing.Fingerprint;
using OutWit.Common.Licensing.Requests;
using OutWit.Common.Licensing.Validation;

namespace OutWit.Common.Licensing
{
    /// <summary>
    /// Evaluates the installed licences and exposes the result.
    /// </summary>
    public sealed class LicenseService : ILicenseService
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

        #region Fields

        private readonly LicensingOptions m_options;
        private readonly LicenseValidator m_validator;

        private IReadOnlyList<LicenseFactor> m_factors = Array.Empty<LicenseFactor>();

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
                unrecognisedKeys: Array.Empty<string>());
        }

        #endregion

        #region ILicenseService

        public LicenseState State { get; private set; }

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
            m_factors = await m_options.BindingProvider.CollectAsync().ConfigureAwait(false);

            var fingerprint = FingerprintCodec.Encode(m_options.FingerprintPrefix, m_factors);
            var utcNow = m_options.Clock();

            var state = m_options.Store.ReadState();

            if (m_options.ClockGuard.IsTampered(state, utcNow))
            {
                State = new LicenseState(
                    LicenseValidationResult.Failure(LicenseStatus.ClockTampered),
                    isDemo: false,
                    fingerprint,
                    Array.Empty<string>());

                return;
            }

            m_options.Store.WriteState(m_options.ClockGuard.Observe(state, utcNow));

            State = Evaluate(fingerprint, state.FirstRunUtc == default ? utcNow : state.FirstRunUtc, utcNow);
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

        #region Tools

        private LicenseState Evaluate(string fingerprint, DateTime firstRunUtc, DateTime utcNow)
        {
            var results = m_options.Store
                .ReadTokens()
                .Select(token => m_validator.Validate(token, m_factors, utcNow))
                .ToList();

            var valid = results.Where(result => result.IsValid).ToList();

            // Supersession is honoured only from licences that are themselves
            // valid right now. A document cannot retire another one before its
            // own start date, and a forged one cannot retire anything at all.
            var superseded = new HashSet<string>(
                valid.SelectMany(result => result.Payload!.Supersedes),
                StringComparer.OrdinalIgnoreCase);

            var effective = valid
                .Where(result => !superseded.Contains(result.Payload!.Id))
                .OrderByDescending(result => result.Payload!.IsUnlimited)
                .ThenByDescending(result => result.Payload!.ExpiresUtc ?? DateTime.MaxValue)
                .ThenByDescending(result => result.Payload!.IssuedUtc)
                .FirstOrDefault();

            if (effective != null)
                return Accept(effective, isDemo: false, fingerprint);

            // Demo applies only to a host that has never had a licence at all.
            // Once one has been installed, that licence's own refusal is the
            // actionable answer: telling a customer whose purchase lapsed that
            // "the demo period has ended" hides the expiry date, the customer
            // name and the renewal they actually need.
            if (results.Count == 0)
            {
                var demo = TryDemo(fingerprint, firstRunUtc, utcNow);
                if (demo != null)
                    return demo;
            }

            return new LicenseState(BestFailure(results), isDemo: false, fingerprint, Array.Empty<string>());
        }

        private LicenseState? TryDemo(string fingerprint, DateTime firstRunUtc, DateTime utcNow)
        {
            if (m_options.Demo == null)
                return null;

            var payload = DemoLicenseFactory.Create(m_options.Product, m_options.Demo, firstRunUtc);

            var result = payload.ExpiresUtc > utcNow
                ? LicenseValidationResult.Valid(payload)
                : LicenseValidationResult.Failure(LicenseStatus.Expired, payload);

            return new LicenseState(result, isDemo: true, fingerprint, Unrecognised(payload), payload.ExpiresUtc);
        }

        private LicenseState Accept(LicenseValidationResult result, bool isDemo, string fingerprint)
        {
            return new LicenseState(result, isDemo, fingerprint, Unrecognised(result.Payload));
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
    }
}
