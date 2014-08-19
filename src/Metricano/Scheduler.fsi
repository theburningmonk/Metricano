namespace Metricano

open System

type TimeUnit = 
    | Minute = 0uy
    | Hour   = 0uy

type IEvery =
    abstract member Value  : float
    abstract member Minute : unit -> IPublishSchedule
    abstract member Hour   : unit -> IPublishSchedule

and IPublishSchedule =
    abstract member Value  : float
    abstract member Unit   : TimeUnit
    abstract member With   : IMetricsPublisher -> unit

[<RequireQualifiedAccess>]
module Publish =
    [<Microsoft.FSharp.Core.CompiledNameAttribute("Every")>]
    val every   : float -> IEvery

    [<Microsoft.FSharp.Core.CompiledNameAttribute("Stop")>]
    val stop    : unit -> unit

[<AutoOpen>]
module FSharpTools =
    val minutes     : (float -> IPublishSchedule)
    val hours       : (float -> IPublishSchedule)
    val every       : float -> (float -> IPublishSchedule) -> IPublishSchedule
    val publishWith : IMetricsPublisher -> IPublishSchedule -> unit