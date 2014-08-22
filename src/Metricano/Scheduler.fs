namespace Metricano

open System
open System.Linq
open System.Collections.Concurrent
open System.Threading

[<RequireQualifiedAccess>]
module Publish =
    let stopped    = ref 0L
    let publishers = new ConcurrentBag<IMetricsPublisher>()

    [<Microsoft.FSharp.Core.CompiledNameAttribute("Interval")>]
    let interval   = TimeSpan.FromSeconds 1.0

    let publish metrics (publisher : IMetricsPublisher) =
        try
            publisher.Publish metrics |> ignore
        with
        | :? ObjectDisposedException -> ()

    let flush      = fun _ ->
        let metrics = MetricsAgent.Flush().Result
        publishers.ToArray() |> Array.iter (publish metrics)
    let timer      = new Timer(flush, null, interval, interval)

    [<Microsoft.FSharp.Core.CompiledNameAttribute("With")>]
    let pubWith (publisher : IMetricsPublisher) = 
        match Interlocked.Read(stopped) with
        | 0L -> publishers.Add(publisher)
        | _  -> failwithf "Publisher has been stopped"

    [<Microsoft.FSharp.Core.CompiledNameAttribute("Stop")>]
    let stop () =
        match Interlocked.Increment(stopped) with
        | 1L -> timer.Dispose()
                flush()
                publishers.ToArray() |> Array.iter (fun pub -> pub.Dispose())
        | _  -> ()