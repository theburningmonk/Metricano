namespace Metricano

open System
open System.Collections.Concurrent
open System.Threading

type IPublishSchedule =
    abstract member And : IMetricsPublisher -> IPublishSchedule

[<AutoOpen>]
module Publish =
    let stopped    = ref 0L
    let publishers = new ConcurrentBag<IMetricsPublisher>()
    let flush      = fun _ -> 
        let metrics = MetricsAgent.Flush().Result
        publishers.ToArray() |> Array.iter (fun pub -> pub.Publish metrics |> ignore)
    let timer      = 
        let ts = TimeSpan.FromMinutes 1.0
        new Timer(flush, null, ts, ts)

    [<Microsoft.FSharp.Core.CompiledNameAttribute("With")>]
    let pubWith (publisher : IMetricsPublisher) = 
        match Interlocked.Read(stopped) with
        | 0L -> publishers.Add(publisher)
        | _  -> failwithf "Publisher has been stopped"

    [<Microsoft.FSharp.Core.CompiledNameAttribute("Stop")>]
    let stop () =
        match Interlocked.Increment(stopped) with
        | 1L -> flush()
                timer.Dispose()
        | _  -> ()