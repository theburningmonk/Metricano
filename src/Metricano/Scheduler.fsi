namespace Metricano

open System

type IPublishSchedule =
    abstract member And : IMetricsPublisher -> IPublishSchedule

[<AutoOpen>]
module Publish =
    [<Microsoft.FSharp.Core.CompiledNameAttribute("With")>]
    val pubWith : IMetricsPublisher -> unit

    [<Microsoft.FSharp.Core.CompiledNameAttribute("Stop")>]
    val stop    : unit -> unit