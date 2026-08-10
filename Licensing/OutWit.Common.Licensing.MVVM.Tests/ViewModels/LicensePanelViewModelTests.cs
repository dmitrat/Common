using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using OutWit.Common.Licensing.MVVM.Tests.Mock;
using OutWit.Common.Licensing.MVVM.ViewModels;
using OutWit.Common.Licensing.Snapshot;
using OutWit.Common.Licensing.Validation;

namespace OutWit.Common.Licensing.MVVM.Tests.ViewModels
{
    /// <summary>
    /// The panel, driven entirely through the gateway — no key ring, no store,
    /// no machine. That it can be tested this way is the point: whatever fits
    /// behind <see cref="ILicenseGateway"/> can drive this panel, which is what
    /// lets one screen serve a desktop app and a remote admin page.
    /// </summary>
    [TestFixture]
    public sealed class LicensePanelViewModelTests
    {
        private static readonly DateTime NOW = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

        private LicenseGatewayMock m_gateway = null!;

        [SetUp]
        public void SetUp()
        {
            m_gateway = new LicenseGatewayMock(Snapshot());
        }

        #region Binding Tests

        [Test]
        public void PanelRendersFromTheFirstFrameTest()
        {
            // No fire-and-forget load in the constructor: a panel whose first
            // frame is empty is exactly the "not evaluated yet" state the
            // runtime was designed not to have.
            using var panel = Panel();

            Assert.Multiple(() =>
            {
                Assert.That(panel.Mode, Is.EqualTo(LicenseMode.Licensed));
                Assert.That(panel.ModeText, Is.EqualTo("Licensed"));
                Assert.That(panel.Customer, Is.EqualTo("ACME GmbH"));
                Assert.That(panel.Edition, Is.EqualTo("Enterprise"));
                Assert.That(panel.Fingerprint, Is.EqualTo("TST-1234-5678"));
                Assert.That(panel.CanRun, Is.True);
                Assert.That(panel.Grants, Has.Count.EqualTo(2));
                Assert.That(panel.Installed, Has.Count.EqualTo(1));
            });
        }

        [Test]
        public void MissingValuesReadAsADashRatherThanAsNothingTest()
        {
            m_gateway = new LicenseGatewayMock(LicenseSnapshot.Empty());

            using var panel = Panel();

            Assert.Multiple(() =>
            {
                Assert.That(panel.Edition, Is.EqualTo("—"));
                Assert.That(panel.Customer, Is.EqualTo("—"));
            });
        }

        [Test]
        public void NotifyRaisesPropertyChangedOnTheGenericPanelTest()
        {
            // The panel is generic and its notifications are woven in, so this
            // asserts the weaving actually reached a generic type rather than
            // silently producing a panel that never updates.
            using var panel = Panel();

            var changed = new List<string?>();
            panel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            panel.PastedToken = "header.payload.signature";

            Assert.That(changed, Does.Contain(nameof(panel.PastedToken)));
        }

        [Test]
        public void StateChangingOnItsOwnReachesThePanelTest()
        {
            // The periodic re-evaluation crossing an expiry, with nobody at the
            // keyboard — the case the whole event chain exists for.
            using var panel = Panel();

            m_gateway.Push(Snapshot(mode: LicenseMode.Restricted, status: LicenseStatus.Expired, days: -1));

            Assert.Multiple(() =>
            {
                Assert.That(panel.Mode, Is.EqualTo(LicenseMode.Restricted));
                Assert.That(panel.CanRun, Is.False);
                Assert.That(panel.Severity, Is.EqualTo(LicenseSeverity.Error));
            });
        }

        [Test]
        public void DisposedPanelStopsListeningTest()
        {
            var panel = Panel();
            panel.Dispose();

            m_gateway.Push(Snapshot(mode: LicenseMode.Restricted, status: LicenseStatus.Expired, days: -1));

            Assert.That(panel.Mode, Is.EqualTo(LicenseMode.Licensed));
        }

        #endregion

        #region Severity Tests

        [Test]
        public void LicensedWithRoomToSpareSaysNothingTest()
        {
            using var panel = Panel();

            Assert.That(panel.Severity, Is.EqualTo(LicenseSeverity.None));
        }

        [Test]
        public void LicensedInsideTheWarningWindowWarnsTest()
        {
            m_gateway = new LicenseGatewayMock(Snapshot(days: 20));

            using var panel = Panel();

            Assert.That(panel.Severity, Is.EqualTo(LicenseSeverity.Warning));
        }

        [Test]
        public void UnlimitedNeverWarnsTest()
        {
            m_gateway = new LicenseGatewayMock(Snapshot(days: null, expires: null));

            using var panel = Panel();

            Assert.Multiple(() =>
            {
                Assert.That(panel.Severity, Is.EqualTo(LicenseSeverity.None));
                Assert.That(panel.ExpiryText, Is.EqualTo("unlimited"));
            });
        }

        [Test]
        public void DemoIsInformationalUntilItIsNearlyOverTest()
        {
            m_gateway = new LicenseGatewayMock(Snapshot(mode: LicenseMode.Demo, isDemo: true, days: 25));
            using var relaxed = Panel();

            m_gateway = new LicenseGatewayMock(Snapshot(mode: LicenseMode.Demo, isDemo: true, days: 3));
            using var pressing = Panel();

            Assert.Multiple(() =>
            {
                Assert.That(relaxed.Severity, Is.EqualTo(LicenseSeverity.Info));
                Assert.That(pressing.Severity, Is.EqualTo(LicenseSeverity.Warning));
            });
        }

        [Test]
        public void GraceIsLoudTest()
        {
            // It is not a comfortable state to sit in — it is a fortnight of
            // borrowed time that somebody has to spend.
            m_gateway = new LicenseGatewayMock(Snapshot(mode: LicenseMode.Grace, status: LicenseStatus.Expired, days: -3));

            using var panel = Panel();

            Assert.Multiple(() =>
            {
                Assert.That(panel.Severity, Is.EqualTo(LicenseSeverity.Error));
                Assert.That(panel.IsGrace, Is.True);
                Assert.That(panel.CanRun, Is.True, "Grace still allows work.");
                Assert.That(panel.ExpiryText, Does.Contain("3 day(s) ago"));
            });
        }

        #endregion

        #region Install Tests

        [Test]
        public void InstallIsDisabledUntilThereIsSomethingToInstallTest()
        {
            using var panel = Panel();

            Assert.That(panel.CanInstall, Is.False);

            panel.PastedToken = "  ";
            Assert.That(panel.CanInstall, Is.False, "Whitespace is not a licence.");

            panel.PastedToken = "header.payload.signature";
            Assert.That(panel.CanInstall, Is.True);
        }

        [Test]
        public async Task AcceptedLicenceClearsTheBoxTest()
        {
            using var panel = Panel();
            panel.PastedToken = "header.payload.signature";

            await panel.InstallAsync();

            Assert.Multiple(() =>
            {
                Assert.That(m_gateway.Installed, Has.Count.EqualTo(1));
                Assert.That(panel.PastedToken, Is.Empty);
                Assert.That(panel.InstallSucceeded, Is.True);
            });
        }

        [Test]
        public async Task RefusedLicenceKeepsWhatWasPastedTest()
        {
            // A customer who pasted half a licence still has the half they
            // pasted to look at.
            m_gateway.NextOutcome = LicenseInstallOutcome.Rejected(
                LicenseStatus.BindingMismatch, "This licence was issued for a different machine.");

            using var panel = Panel();
            panel.PastedToken = "header.payload.signature";

            await panel.InstallAsync();

            Assert.Multiple(() =>
            {
                Assert.That(panel.PastedToken, Is.Not.Empty);
                Assert.That(panel.InstallSucceeded, Is.False);
                Assert.That(panel.InstallMessage, Does.Contain("different machine"));
            });
        }

        [Test]
        public async Task AGatewayThatThrowsBecomesAMessageTest()
        {
            // A licence panel is the screen a customer reaches because
            // something is already wrong. It is the last place that may throw.
            m_gateway.Throws = true;

            using var panel = Panel();
            panel.PastedToken = "header.payload.signature";

            await panel.InstallAsync();

            Assert.Multiple(() =>
            {
                Assert.That(panel.InstallSucceeded, Is.False);
                Assert.That(panel.InstallMessage, Does.Contain("read-only"));
                Assert.That(panel.IsBusy, Is.False, "The busy flag must come down however it ended.");
            });
        }

        #endregion

        #region Remove Tests

        [Test]
        public async Task RemovingActsOnTheSelectedDocumentTest()
        {
            using var panel = Panel();
            panel.SelectedDocument = panel.Installed.First();

            Assert.That(panel.CanRemove, Is.True);

            await panel.RemoveAsync();

            Assert.Multiple(() =>
            {
                Assert.That(m_gateway.Removed, Is.EqualTo(new[] { "licence-1" }));
                Assert.That(panel.SelectedDocument, Is.Null);
                Assert.That(panel.CanRemove, Is.False);
            });
        }

        [Test]
        public async Task ALicenceTheProductDoesNotOwnSaysSoTest()
        {
            m_gateway.NextRemoveResult = false;

            using var panel = Panel();
            panel.SelectedDocument = panel.Installed.First();

            await panel.RemoveAsync();

            Assert.Multiple(() =>
            {
                Assert.That(panel.InstallSucceeded, Is.False);
                Assert.That(panel.InstallMessage, Does.Contain("not installed by this product"));
            });
        }

        #endregion

        #region Seam Tests

        [Test]
        public void CommandsWithoutASeamAreVisiblyDisabledTest()
        {
            // Never a silent no-op. A control that does nothing when clicked is
            // reported as a broken product, which is the failure surface the
            // design refuses everywhere else too.
            using var panel = Panel();

            Assert.Multiple(() =>
            {
                Assert.That(panel.CanCopy, Is.False);
                Assert.That(panel.CanUseFiles, Is.False);
                Assert.That(panel.CopyFingerprintCmd.CanExecute(null), Is.False);
                Assert.That(panel.OpenLicenseFileCmd.CanExecute(null), Is.False);
            });
        }

        [Test]
        public async Task ClipboardSeamCopiesTheFingerprintTest()
        {
            var clipboard = new LicenseClipboardMock();

            using var panel = Panel(clipboard: clipboard);

            Assert.That(panel.CanCopy, Is.True);

            panel.CopyFingerprintCmd.Execute(null);
            await Task.Yield();

            Assert.That(clipboard.Copied, Is.EqualTo(new[] { "TST-1234-5678" }));
        }

        [Test]
        public async Task FileSeamOpensALicenceAndInstallsItTest()
        {
            var files = new LicenseFileTransferMock { Opened = "  header.payload.signature  " };

            using var panel = Panel(files: files);

            await panel.OpenLicenseFileAndInstallAsync();

            Assert.Multiple(() =>
            {
                Assert.That(m_gateway.Installed, Is.EqualTo(new[] { "header.payload.signature" }));
                Assert.That(panel.InstallSucceeded, Is.True);
            });
        }

        [Test]
        public async Task CancelledFileDialogInstallsNothingTest()
        {
            var files = new LicenseFileTransferMock { Opened = null };

            using var panel = Panel(files: files);

            await panel.OpenLicenseFileAndInstallAsync();

            Assert.That(m_gateway.Installed, Is.Empty);
        }

        [Test]
        public async Task RequestIsBuiltThenSavedUnderASelfIdentifyingNameTest()
        {
            var files = new LicenseFileTransferMock();

            using var panel = Panel(files: files);
            panel.RequestContact = "it@acme.example";
            panel.RequestNotes = "PO-2026-0417";

            await panel.CreateRequestAsync();

            Assert.Multiple(() =>
            {
                Assert.That(panel.RequestBlob, Is.Not.Empty);
                Assert.That(m_gateway.Contact, Is.EqualTo("it@acme.example"));
                Assert.That(m_gateway.Notes, Is.EqualTo("PO-2026-0417"));
                Assert.That(panel.CanSaveRequest, Is.True);
            });

            await panel.SaveRequestAsync();

            Assert.That(files.Saved.Single().FileName, Does.EndWith(".owlreq"));
        }

        [Test]
        public void SavingIsDisabledBeforeThereIsARequestTest()
        {
            using var panel = Panel(files: new LicenseFileTransferMock());

            Assert.That(panel.CanSaveRequest, Is.False);
        }

        #endregion

        #region Tools

        private LicensePanelViewModelStandalone Panel(
            LicenseClipboardMock? clipboard = null,
            LicenseFileTransferMock? files = null)
        {
            return new LicensePanelViewModelStandalone(m_gateway, clipboard, files);
        }

        private static LicenseSnapshot Snapshot(
            LicenseMode mode = LicenseMode.Licensed,
            LicenseStatus status = LicenseStatus.Valid,
            bool isDemo = false,
            int? days = 90,
            DateTime? expires = null)
        {
            return new LicenseSnapshot
            {
                Mode = mode,
                Status = status,
                Description = "Licensed to ACME GmbH.",
                Product = "TestProduct",
                ProductVersion = "1.5.0",
                Fingerprint = "TST-1234-5678",
                LicenseId = "licence-1",
                Edition = "Enterprise",
                CustomerName = "ACME GmbH",
                IsDemo = isDemo,
                CanRun = mode is LicenseMode.Licensed or LicenseMode.Demo or LicenseMode.Grace,
                EvaluatedUtc = NOW,
                ExpiresUtc = days == null ? expires : NOW.AddDays(days.Value),
                DaysRemaining = days,
                GracePolicy = "No renewal grace.",
                Grants = new List<LicenseGrant>
                {
                    new() { Key = "sso", Description = "Single sign-on", Kind = LicenseGrantKind.Feature, IsGranted = true },
                    new() { Key = "maxNodes", Description = "Compute nodes", Kind = LicenseGrantKind.Limit, IsGranted = true, Value = 50 }
                },
                Installed = new List<LicenseDocument>
                {
                    new() { Id = "licence-1", Edition = "Enterprise", KeyId = "test-key-1", Status = LicenseStatus.Valid, IsEffective = true }
                }
            };
        }

        #endregion
    }
}
