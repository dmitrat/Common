using System;
using System.Collections.Generic;
using System.Linq;
using OutWit.Common.Licensing.Validation;

namespace OutWit.Common.Licensing.Snapshot
{
    /// <summary>
    /// Projects an evaluated <see cref="LicenseState"/> into the flat
    /// <see cref="LicenseSnapshot"/> a panel or a health endpoint reads.
    /// </summary>
    internal static class LicenseSnapshotFactory
    {
        #region Functions

        /// <summary>
        /// Builds the snapshot.
        /// <para>
        /// <paramref name="hasFeature"/> and <paramref name="limit"/> are the
        /// service's own accessors rather than a second reading of the payload.
        /// A snapshot that computed grants for itself would be free to disagree
        /// with the service that gates on them — and the disagreement would show
        /// up as a panel saying a customer has something the product refuses to
        /// give them.
        /// </para>
        /// </summary>
        public static LicenseSnapshot Create(
            LicensingOptions options,
            LicenseState state,
            IReadOnlyList<LicenseDocument> installed,
            Func<string, bool> hasFeature,
            Func<string, long> limit)
        {
            return new LicenseSnapshot
            {
                Mode = state.Mode,
                Status = state.Status,
                Description = state.Describe(),

                Product = options.Product,
                ProductVersion = options.ProductVersion?.ToString() ?? string.Empty,
                Fingerprint = state.Fingerprint,

                LicenseId = state.Payload?.Id ?? string.Empty,
                Edition = state.Payload?.Edition ?? string.Empty,
                CustomerName = state.Payload?.Customer?.Name ?? string.Empty,

                IsDemo = state.IsDemo,
                IsClockSuspect = state.IsClockSuspect,
                CanRun = state.CanRun,

                EvaluatedUtc = state.EvaluatedUtc,
                ExpiresUtc = state.ExpiresUtc,
                DaysRemaining = state.DaysRemaining,

                GraceExpiresUtc = state.GraceExpiresUtc,
                GracePolicy = state.DescribeGracePolicy(),

                Grants = BuildGrants(options, hasFeature, limit),
                UnrecognisedKeys = state.UnrecognisedKeys.ToList(),
                Installed = installed
            };
        }

        /// <summary>Builds one document line from a validated token.</summary>
        public static LicenseDocument Describe(Crypto.LicenseToken? token, LicenseValidationResult result, bool isEffective)
        {
            var payload = result.Payload;

            return new LicenseDocument
            {
                Id = payload?.Id ?? string.Empty,
                Edition = payload?.Edition ?? "(unreadable)",
                KeyId = token?.Header.KeyId ?? string.Empty,
                CustomerName = payload?.Customer?.Name ?? string.Empty,
                NotBeforeUtc = payload?.NotBeforeUtc ?? default,
                ExpiresUtc = payload?.ExpiresUtc,
                Status = result.Status,
                IsEffective = isEffective
            };
        }

        #endregion

        #region Tools

        /// <summary>
        /// One line per <b>declared</b> key, not per granted one. A capability
        /// the customer bought and did not receive has to be visible as a "no"
        /// beside its own description; listing only what the licence carries
        /// would render that case as an empty space.
        /// </summary>
        private static IReadOnlyList<LicenseGrant> BuildGrants(
            LicensingOptions options,
            Func<string, bool> hasFeature,
            Func<string, long> limit)
        {
            var grants = new List<LicenseGrant>();

            foreach (var feature in options.Vocabulary.Features)
            {
                grants.Add(new LicenseGrant
                {
                    Key = feature.Key,
                    Description = feature.Value,
                    Kind = LicenseGrantKind.Feature,
                    IsGranted = hasFeature(feature.Key)
                });
            }

            foreach (var declared in options.Vocabulary.Limits)
            {
                grants.Add(new LicenseGrant
                {
                    Key = declared.Key,
                    Description = declared.Value.Description,
                    Kind = LicenseGrantKind.Limit,
                    IsGranted = true,
                    Value = limit(declared.Key)
                });
            }

            return grants;
        }

        #endregion
    }
}
