using System.Windows.Markup;

// One namespace for the markup, so a window declares
//   xmlns:n="https://schemas.outwit.io/navigation"
// and reaches every control without naming an assembly or a CLR namespace — the same URI the
// Avalonia package uses, so the two markups differ by as little as possible.
[assembly: XmlnsDefinition("https://schemas.outwit.io/navigation", "OutWit.Common.MVVM.Navigation.WPF.Controls")]
[assembly: XmlnsPrefix("https://schemas.outwit.io/navigation", "nav")]
