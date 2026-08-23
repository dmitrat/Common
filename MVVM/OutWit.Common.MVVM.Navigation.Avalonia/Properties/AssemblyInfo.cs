using Avalonia.Metadata;

// One namespace for the markup, so a window declares
//   xmlns:n="https://schemas.outwit.io/navigation"
// and reaches every control without naming an assembly or a CLR namespace. A dedicated URI
// rather than Avalonia's own: these controls are not part of the framework, and a consumer
// should be able to tell where NavigationOutlet came from by looking at the prefix.
[assembly: XmlnsDefinition("https://schemas.outwit.io/navigation", "OutWit.Common.MVVM.Navigation.Avalonia.Controls")]
[assembly: XmlnsPrefix("https://schemas.outwit.io/navigation", "nav")]
