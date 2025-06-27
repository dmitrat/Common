using System;
using System.Collections.Generic;
using System.Text;
using OutWit.Common.Settings.Sections;
using OutWit.Common.Settings.Tests.Utils;
using NUnit.Framework;

namespace OutWit.Common.Settings.Tests
{
    [TestFixture]
    public class SettingAspectTests
    {
        [Test]
        public void ConstructorTest()
        {
            var setting = new TestSettings();
            Assert.That(setting.CurrentLanguage, Is.EqualTo("English"));
            Assert.That(setting.UsePcSpeaker, Is.EqualTo(true));
            Assert.That(setting.IsNavigationBarFixed, Is.EqualTo(true));
            Assert.That(setting.ShowSplashScreen, Is.EqualTo(false));
            Assert.That(setting.HideWhileLoading, Is.EqualTo(true));
            Assert.That(setting.TestEnum, Is.EqualTo(TestEnum.Option1));

            setting.IsNavigationBarFixed = false;
            Assert.That(setting.IsNavigationBarFixed, Is.EqualTo(false));

            setting.CurrentLanguage = "Russian";
            Assert.That(setting.CurrentLanguage, Is.EqualTo("Russian"));

            setting.TestEnum = TestEnum.Option2;
            Assert.That(setting.TestEnum, Is.EqualTo(TestEnum.Option2));
        }


        [Test]
        public void MergeTest()
        {
            var setting = new TestSettings();
            var value = setting.General["UsePcSpeaker"];
        }
    }
}
