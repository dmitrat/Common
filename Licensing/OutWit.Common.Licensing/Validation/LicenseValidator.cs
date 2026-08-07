using System;
using System.Collections.Generic;
using OutWit.Common.Licensing.Abstract;
using OutWit.Common.Licensing.Binding;
using OutWit.Common.Licensing.Crypto;
using OutWit.Common.Licensing.Keys;

namespace OutWit.Common.Licensing.Validation
{
    /// <summary>
    /// Decides whether a licence token may be honoured by this build, on this
    /// host, right now.
    /// <para>
    /// Deliberately free of ambient state: the clock is passed in and the host's
    /// factors are passed in, so the rules that decide whether a customer can
    /// work are testable without a machine, a file or a wait.
    /// </para>
    /// </summary>
    public sealed class LicenseValidator
    {
        #region Constants

        /// <summary>
        /// Longest term a <see cref="LicenseKeyPolicy.TrialOnly"/> key may
        /// grant. Generous enough for any evaluation, finite enough that a
        /// leaked trial key is an annoyance rather than a loss.
        /// </summary>
        public const int TRIAL_MAX_DAYS = 180;

        #endregion

        #region Fields

        private readonly ILicenseKeyRing m_keyRing;
        private readonly string m_product;
        private readonly Version? m_productVersion;

        #endregion

        #region Constructors

        public LicenseValidator(ILicenseKeyRing keyRing, string product, Version? productVersion = null)
        {
            m_keyRing = keyRing;
            m_product = product;
            m_productVersion = productVersion;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Validates a raw token against this build, the supplied host factors
        /// and the supplied instant.
        /// </summary>
        /// <param name="token">The licence token as received.</param>
        /// <param name="presentFactors">Hashed factors observed on this host.</param>
        /// <param name="utcNow">The instant to judge the term against.</param>
        public LicenseValidationResult Validate(string? token, IReadOnlyList<LicenseFactor>? presentFactors, DateTime utcNow)
        {
            if (string.IsNullOrWhiteSpace(token))
                return LicenseValidationResult.Failure(LicenseStatus.Missing);

            var parsed = LicenseToken.Parse(token);
            if (parsed == null)
                return LicenseValidationResult.Failure(LicenseStatus.Malformed);

            return ValidateToken(parsed, presentFactors, utcNow);
        }

        /// <summary>
        /// Validates an already-parsed token. Named apart from
        /// <see cref="Validate(string, IReadOnlyList{LicenseFactor}, DateTime)"/>
        /// rather than overloaded, because an overload set that is ambiguous on
        /// a literal <c>null</c> is a trap for every caller after the first.
        /// </summary>
        public LicenseValidationResult ValidateToken(LicenseToken token, IReadOnlyList<LicenseFactor>? presentFactors, DateTime utcNow)
        {
            var payload = token.Payload;

            var key = m_keyRing.Find(token.Header.KeyId);
            if (key == null)
                return LicenseValidationResult.Failure(LicenseStatus.UnknownKeyId, payload,
                    $"kid '{token.Header.KeyId}' is not in this build's key ring.");

            // The header names an algorithm; the key ring decides it. A token
            // whose header disagrees is refused rather than honoured under
            // whichever primitive it asked for — that is the substitution attack.
            if (key.Algorithm == LicenseAlgorithm.None || token.Header.Algorithm != key.Algorithm)
                return LicenseValidationResult.Failure(LicenseStatus.SignatureInvalid, payload,
                    $"Header claims '{token.Header.Algorithm}' but kid '{key.KeyId}' is registered for '{key.Algorithm}'.");

            // Scope before signature: a key that is not allowed to speak for this
            // product should be refused on those grounds, whatever it signed.
            if (!key.CoversProduct(payload.Product))
                return LicenseValidationResult.Failure(LicenseStatus.ExceedsKeyPolicy, payload,
                    $"kid '{key.KeyId}' is not scoped to product '{payload.Product}'.");

            if (!LicenseSigner.Verify(token.SigningInput, token.Signature, key.PublicKeyPem, key.Algorithm))
                return LicenseValidationResult.Failure(LicenseStatus.SignatureInvalid, payload);

            if (!string.Equals(payload.Product, m_product, StringComparison.OrdinalIgnoreCase))
                return LicenseValidationResult.Failure(LicenseStatus.WrongProduct, payload,
                    $"Licence is for '{payload.Product}', this product is '{m_product}'.");

            var policyFailure = CheckKeyPolicy(key, payload);
            if (policyFailure != null)
                return policyFailure;

            if (!LicenseVersionRange.Parse(payload.AppVersionRange).Matches(m_productVersion))
                return LicenseValidationResult.Failure(LicenseStatus.WrongVersion, payload,
                    $"Running version {m_productVersion?.ToString() ?? "unknown"} is outside '{payload.AppVersionRange}'.");

            // Binding before term: "this is not your licence" is a more
            // fundamental answer than "your licence ran out", and sends the
            // customer to the right request.
            if (!LicenseBindingMatcher.Matches(payload.Binding, presentFactors))
            {
                var matched = LicenseBindingMatcher.CountMatches(payload.Binding, presentFactors);
                var required = LicenseBindingMatcher.RequiredMatches(payload.Binding);

                return LicenseValidationResult.Failure(LicenseStatus.BindingMismatch, payload,
                    $"{matched} of {required} required binding factors match this host.");
            }

            if (payload.NotBeforeUtc > utcNow)
                return LicenseValidationResult.Failure(LicenseStatus.NotYetValid, payload);

            if (payload.ExpiresUtc != null && payload.ExpiresUtc <= utcNow)
                return LicenseValidationResult.Failure(LicenseStatus.Expired, payload);

            return LicenseValidationResult.Valid(payload);
        }

        #endregion

        #region Tools

        /// <summary>
        /// Enforces what the signing key is permitted to grant, independently of
        /// what the payload claims. A perfectly-signed licence still fails here
        /// when the key was not allowed to issue it.
        /// </summary>
        private static LicenseValidationResult? CheckKeyPolicy(LicenseKeyInfo key, LicensePayload payload)
        {
            if (key.Policy != LicenseKeyPolicy.TrialOnly)
                return null;

            if (payload.IsUnlimited)
                return LicenseValidationResult.Failure(LicenseStatus.ExceedsKeyPolicy, payload,
                    $"kid '{key.KeyId}' is trial-only and may not grant an unlimited term.");

            var days = (payload.ExpiresUtc!.Value - payload.NotBeforeUtc).TotalDays;
            if (days > TRIAL_MAX_DAYS)
                return LicenseValidationResult.Failure(LicenseStatus.ExceedsKeyPolicy, payload,
                    $"kid '{key.KeyId}' is trial-only; {days:F0} days exceeds the {TRIAL_MAX_DAYS}-day ceiling.");

            return null;
        }

        #endregion
    }
}
