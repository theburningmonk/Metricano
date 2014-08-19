namespace Metricano

open System
open System.Collections.Concurrent
open System.Threading

type TimeUnit = 
    | Minute = 0uy
    | Hour   = 0uy

type IEvery =
    abstract member Value  : double
    abstract member Minute : unit -> IPublishSchedule
    abstract member Hour   : unit -> IPublishSchedule

and IPublishSchedule =
    abstract member Value  : double
    abstract member Unit   : TimeUnit
    abstract member With   : IMetricsPublisher -> unit

[<RequireQualifiedAccess>]
module Publish =
    let stopped   = ref 0L
    let timers    = new ConcurrentBag<Timer>()

    let pubWith (ts : TimeSpan) (publisher : IMetricsPublisher) = 
        match Interlocked.Read(stopped) with
        | 0L -> 
            let callback = fun _ -> publisher.Publish([||])
            let timer = new Timer(callback, null, ts, ts)
            timers.Add(timer)
        | _  -> failwithf "Publisher has been stopped"

    let makeSchedule unit n = 
        let ts = match unit with 
                 | TimeUnit.Minute -> TimeSpan.FromMinutes n
                 | TimeUnit.Hour   -> TimeSpan.FromHours n

        { new IPublishSchedule with
            member x.Value = n
            member x.Unit  = unit
            member x.With publisher = pubWith ts publisher }

    [<Microsoft.FSharp.Core.CompiledNameAttribute("Every")>]
    let every n = 
        { new IEvery with 
            member x.Value     = n
            member x.Minute () = makeSchedule TimeUnit.Minute n
            member x.Hour   () = makeSchedule TimeUnit.Hour n }

    [<Microsoft.FSharp.Core.CompiledNameAttribute("Stop")>]
    let stop () =
        match Interlocked.Increment(stopped) with
        | 1L -> timers.ToArray() |> Array.iter (fun timer -> timer.Dispose())
        | _  -> ()

[<AutoOpen>]
module FSharpTools =
    let minutes = Publish.makeSchedule TimeUnit.Minute
    let hours   = Publish.makeSchedule TimeUnit.Hour
    let every n (f : float -> IPublishSchedule) = f n
    let publishWith publisher (schedule : IPublishSchedule) = schedule.With(publisher)