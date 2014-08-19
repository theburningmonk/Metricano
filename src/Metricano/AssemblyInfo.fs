namespace System
open System.Reflection
open System.Runtime.CompilerServices

[<assembly: AssemblyTitleAttribute("Metricano")>]
[<assembly: AssemblyProductAttribute("Metricano")>]
[<assembly: AssemblyDescriptionAttribute("Agent-based F# library for collecting, aggregating and publishing metrics")>]
[<assembly: AssemblyVersionAttribute("0.1.0")>]
[<assembly: AssemblyFileVersionAttribute("0.1.0")>]
[<assembly: InternalsVisibleToAttribute("Metricano.Tests")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.1.0"
