namespace Metricano.Publisher

open System
open System.Collections.Generic
open System.Linq
open System.Threading
open System.Threading.Tasks

open Metricano
open Metricano.Extensions
open Amazon.CloudWatch
open Amazon.CloudWatch.Model

/// Exception that's thrown when attempting to merge two MetricDatum that cannot be merged
exception MetricDatumMismatch of MetricDatum * MetricDatum

[<AutoOpen>]
module Seq =
    /// Groups a sequence into gropus of at most the specified size
    /// Originally from http://fssnip.net/1o
    let inline groupsOfAtMost (size : int) (s : seq<'v>) : seq<'v[]> =
        seq {
            let en = s.GetEnumerator ()
            let more = ref true
            while !more do
                let group =
                    [|
                        let i = ref 0
                        while !i < size && en.MoveNext () do
                            yield en.Current
                            i := !i + 1
                    |]
                if group.Length = 0
                then more := false
                else yield group
        }

[<AutoOpen>]
module Extensions =
    /// Default function for calcuating delay (in milliseconds) between retries, based on (http://en.wikipedia.org/wiki/Exponential_backoff)
    /// After 8 retries the delay starts to become unreasonable for most scenarios, so cap the delay at that
    let private exponentialDelay =
        let calcDelay retries = 
            let rec sum acc = function | 0 -> acc | n -> sum (acc + n) (n - 1)

            let n = pown 2 retries - 1
            let slots = float (sum 0 n) / float (n + 1)
            int (100.0 * slots)

        let delays = [| 0..8 |] |> Array.map calcDelay

        (fun retries -> delays.[min retries 8])

    type Async with
        /// Starts a computation as a plain task.
        static member StartAsPlainTask (work : Async<unit>) = 
            Task.Factory.StartNew(fun () -> work |> Async.RunSynchronously)

        /// Retries the async computation up to specified number of times. Optionally accepts a function to calculate
        /// the delay in milliseconds between retries, default is exponential delay with a backoff slot of 500ms.
        static member WithRetry (computation : Async<'a>, maxRetries, ?calcDelay) =
            let calcDelay = defaultArg calcDelay exponentialDelay

            let rec loop retryCount =
                async {
                    let! res = computation |> Async.Catch
                    match res with
                    | Choice2Of2 _ when retryCount < maxRetries -> 
                        do! calcDelay retryCount |> Async.Sleep
                        return! loop (retryCount + 1)
                    | _ -> return res
                }
            loop 0

    type Metricano.Metric with
        member this.ToMetricDatum () =
            let dim, unit = 
                match this with
                | Count _    -> new Dimension(Name = "Type", Value = "Count"), StandardUnit.Count
                | TimeSpan _ -> new Dimension(Name = "Type", Value = "TimeSpan"), StandardUnit.Milliseconds

            new MetricDatum(
                MetricName = this.Name,
                Unit       = unit,
                Timestamp  = this.TimeStamp,
                Dimensions = new List<Dimension>([| dim |]),
                StatisticValues = new StatisticSet(
                    Maximum     = this.Max,
                    Minimum     = this.Min,
                    Sum         = this.Sum,
                    SampleCount = float this.SampleCount
                ))
               
[<RequireQualifiedAccess>]
module Constants =
    // cloud watch limits the MetricDatum list to a size of 20 per request
    let putMetricDataListSize = 20

type Message =
    | AddMetricData     of Metricano.Metric[]
    | Publish
    | SyncPublish       of AsyncReplyChannel<unit>

type CloudWatchPublisher (rootNamespace : string, client : IAmazonCloudWatch) =
    let onPutMetricError = new Event<Exception>()

    let combine (left : MetricDatum) (right : MetricDatum) =
        if left.MetricName = right.MetricName &&
            left.Unit       = right.Unit 
        then left.StatisticValues.SampleCount <- left.StatisticValues.SampleCount + right.StatisticValues.SampleCount
             left.StatisticValues.Sum         <- left.StatisticValues.Sum + right.StatisticValues.Sum
             left.StatisticValues.Maximum     <- max left.StatisticValues.Maximum right.StatisticValues.Maximum
             left.StatisticValues.Minimum     <- min left.StatisticValues.Minimum right.StatisticValues.Minimum
        else raise <| MetricDatumMismatch(left, right)

    let send (datum : MetricDatum[]) = async {
        let req = new PutMetricDataRequest(Namespace  = rootNamespace,
                                           MetricData = datum.ToList())
        let sendAsync = client.PutMetricDataAsync(req) |> Async.AwaitTask
        let! res = Async.WithRetry(sendAsync, 3)
        match res with
        | Choice1Of2 _   -> ()
        | Choice2Of2 exn -> onPutMetricError.Trigger exn
    }

    let sendAll (groups : MetricDatum[] seq) = 
        groups 
        |> Seq.map send
        |> Async.Parallel
        |> Async.Ignore

    let agent = Agent<Message>.StartSupervised(fun inbox ->
        let metricData = new Dictionary<string * MetricType, MetricDatum>()
        let add (metric : Metricano.Metric) = 
            let key, newDatum = (metric.Name, metric.Type), metric.ToMetricDatum()
            match metricData.TryGetValue key with
            | true, datum -> combine datum newDatum
            | _ -> metricData.[key] <- newDatum

        let pub () =
            metricData.Values
            |> Seq.groupsOfAtMost Constants.putMetricDataListSize
            |> sendAll
            |> Async.StartAsPlainTask
            |> (fun task -> task.Wait())

            metricData.Clear()

        let rec loop () = async {
            let! msg = inbox.Receive()

            match msg with
            | AddMetricData metrics -> 
                metrics |> Array.iter add
                return! loop()
            | Publish ->
                pub()
                return! loop()
            | SyncPublish reply ->
                pub()
                reply.Reply()
                return! loop()
        }
        
        loop())

    let putMetricData (metrics : Metricano.Metric[]) = 
        async {
            agent.Post <| AddMetricData metrics
        } 
        |> Async.StartAsPlainTask

    let interval = TimeSpan.FromMinutes 1.0
    let timer    = new Timer((fun _ -> agent.Post Publish), null, interval, interval)
        
    [<CLIEvent>] member this.OnPutMetricError = onPutMetricError.Publish
    
    new(rootNamespace) = new CloudWatchPublisher(rootNamespace, new AmazonCloudWatchClient())
    new(rootNamespace, credentials : Amazon.Runtime.AWSCredentials, region : Amazon.RegionEndpoint) = 
        new CloudWatchPublisher(rootNamespace, new AmazonCloudWatchClient(credentials, region))
    new(rootNamespace, awsAccessKeyId, awsSecretAccessKey, region : Amazon.RegionEndpoint) = 
        new CloudWatchPublisher(rootNamespace, new AmazonCloudWatchClient(awsAccessKeyId, awsSecretAccessKey, region))

    interface IMetricsPublisher with
        member this.Publish metrics = putMetricData metrics
        member this.Dispose ()      = 
            timer.Dispose()
            agent.PostAndReply((fun reply -> SyncPublish reply), 10000) // give it 10 seconds to finish
            (agent :> IDisposable).Dispose()