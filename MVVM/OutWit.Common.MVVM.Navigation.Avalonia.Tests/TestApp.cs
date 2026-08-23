using Avalonia;
using Avalonia.Themes.Fluent;

namespace OutWit.Common.MVVM.Navigation.Avalonia.Tests
{
    /// <summary>
    /// Fluent theme, so ContentControl and Window have templates to build.
    /// </summary>
    public class TestApp : Application
    {
        public override void Initialize()
        {
            Styles.Add(new FluentTheme());
        }
    }
}
