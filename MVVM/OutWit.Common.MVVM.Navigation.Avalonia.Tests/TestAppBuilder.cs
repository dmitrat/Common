using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using OutWit.Common.MVVM.Navigation.Avalonia.Tests;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace OutWit.Common.MVVM.Navigation.Avalonia.Tests
{
    /// <summary>
    /// The headless application every [AvaloniaTest] runs in.
    /// </summary>
    public static class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<TestApp>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions());
        }
    }
}
