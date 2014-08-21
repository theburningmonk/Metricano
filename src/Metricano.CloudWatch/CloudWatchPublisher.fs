namespace Metricano.Publisher

open System
open System.Collections.Generic
open System.Linq
open System.Threading.Tasks

open Metricano
open Amazon.CloudWatch
open Amazon.CloudWatch.Model

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
    type Async with
        /// Starts a computation as a plain task.
        static member StartAsPlainTask (work : Async<unit>) = 
            Task.Factory.StartNew(fun () -> work |> Async.RunSynchronously)

    type Metricano.Metric with
        member this.ToMetricData () =
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

type CloudWatchPublisher (rootNamespace : string, client : IAmazonCloudWatch) =
    let onPutMetricError = new Event<Exception>()

    let send (datum : MetricDatum[]) = async {
        let req = new PutMetricDataRequest(Namespace  = rootNamespace,
                                           MetricData = datum.ToList())
        let! res = client.PutMetricDataAsync(req) |> Async.AwaitTask |> Async.Catch
        match res with
        | Choice1Of2 _   -> ()
        | Choice2Of2 exn -> onPutMetricError.Trigger exn
    }

    let sendAll (groups : MetricDatum[] seq) = 
        groups 
        |> Seq.map send
        |> Async.Parallel
        |> Async.Ignore

    let putMetricData (datum : Metricano.Metric seq) =
        datum 
        |> Seq.map (fun m -> m.ToMetricData())
        |> Seq.groupsOfAtMost Constants.putMetricDataListSize
        |> sendAll
        |> Async.StartAsPlainTask
        
    [<CLIEvent>] member this.OnPutMetricError = onPutMetricError.Publish
    
    new(rootNamespace) = CloudWatchPublisher(rootNamespace, new AmazonCloudWatchClient())
    new(rootNamespace, credentials : Amazon.Runtime.AWSCredentials, region : Amazon.RegionEndpoint) = 
        CloudWatchPublisher(rootNamespace, new AmazonCloudWatchClient(credentials, region))
    new(rootNamespace, awsAccessKeyId, awsSecretAccessKey, region : Amazon.RegionEndpoint) = 
        CloudWatchPublisher(rootNamespace, new AmazonCloudWatchClient(awsAccessKeyId, awsSecretAccessKey, region))

    interface IMetricsPublisher with
        member this.Publish metrics = putMetricData metrics