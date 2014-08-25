namespace Metricano

open System
open System.Collections.Concurrent
open System.Threading

open Metricano.Extensions

[<AutoOpen>]
module Helpers = 
    let swallowExn f = try f () with | _ -> ()

[<Sealed>]
type Publish private () =
    static let publishers = new ConcurrentDictionary<IMetricsAgent, IMetricsPublisher list>()
    static let interval   = TimeSpan.FromSeconds 1.0
    static let stopped    = ref 0

    static let pubWith (metricsAgent : IMetricsAgent) (publisher : IMetricsPublisher) =
        publishers.AddOrUpdate(metricsAgent, [ publisher ], fun _ lst -> publisher::lst)
        |> ignore

    static let flush _ =
        let publish metrics (publisher : IMetricsPublisher) = 
            async {
                let! res = publisher.Publish metrics 
                           |> Async.AwaitPlainTask 
                           |> Async.Catch
                res |> ignore
            }

        let flushInternal (metricsAgent : IMetricsAgent) (publishers: IMetricsPublisher list) =
            async {
                let metrics = metricsAgent.Flush().Result
                let! res    = publishers 
                              |> List.map (publish metrics)
                              |> Async.Parallel
                res |> ignore
            }

        let work = publishers.ToArray()
                   |> Array.map (fun (KeyValue(metricsAgent, publishers)) -> flushInternal metricsAgent publishers)
                   |> Async.Parallel
                   |> Async.Ignore
        
        swallowExn (fun _ -> work |> Async.RunSynchronously)
        
    static let timer = new Timer(flush, null, interval, interval)

    static let stop () =
        match Interlocked.Increment(stopped) with
        | 1 -> timer.Dispose()
               flush()
               publishers.ToArray() 
               |> Array.iter (fun (KeyValue(_, publishers)) -> publishers |> List.iter (fun pub -> pub.Dispose()))
        | _ -> ()

    static member Interval                       = interval
    static member With publisher                 = pubWith MetricsAgent.Default publisher
    static member With (metricsAgent, publisher) = pubWith metricsAgent publisher
    static member Stop ()                        = stop()