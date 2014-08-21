namespace Metricano

open System

[<AutoOpen>]
module Publish =
    [<Microsoft.FSharp.Core.CompiledNameAttribute("Interval")>]
    val interval : TimeSpan

    [<Microsoft.FSharp.Core.CompiledNameAttribute("With")>]
    val pubWith  : IMetricsPublisher -> unit

    [<Microsoft.FSharp.Core.CompiledNameAttribute("Stop")>]
    val stop     : unit -> unit