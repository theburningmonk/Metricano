namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("Metricano.CloudWatch")>]
[<assembly: AssemblyProductAttribute("Metricano.CloudWatch")>]
[<assembly: AssemblyDescriptionAttribute("Publisher for Metricano that publishes metrics to Amazon CloudWatch")>]
[<assembly: AssemblyVersionAttribute("0.2.1")>]
[<assembly: AssemblyFileVersionAttribute("0.2.1")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.2.1"
