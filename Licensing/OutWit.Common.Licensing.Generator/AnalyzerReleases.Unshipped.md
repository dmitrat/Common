; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
OWL001  | OutWit.Licensing | Error   | The product descriptor could not be read
OWL002  | OutWit.Licensing | Error   | The key ring could not be read
OWL003  | OutWit.Licensing | Warning | The key ring will refuse more than it looks like it should
OWL004  | OutWit.Licensing | Error   | Two key rings claim the same product
