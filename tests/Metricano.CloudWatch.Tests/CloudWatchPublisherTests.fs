namespace Metricano.CloudWatch.Tests

open System
open System.Threading
open System.Threading.Tasks

open Amazon.CloudWatch
open Amazon.CloudWatch.Model
open Foq
open FsUnit
open NUnit.Framework
open Metricano
open Metricano.Publisher

[<TestFixture>]
type ``CloudWatchPublisher tests`` () = 
    [<Test>]
    member test.``publish should handle failures`` () =
        let res = async { failwith "boom"; return PutMetricDataResponse() } |> Async.StartAsTask
        let exn : Exception ref = ref null
        let ns  = "test"

        let cloudWatch =
            Mock<Amazon.CloudWatch.IAmazonCloudWatch>()
                .Setup(fun x -> <@ x.PutMetricDataAsync(any(), any()) @>)
                .Returns(res)
                .Create()

        let cloudWatchPub = new CloudWatchPublisher(ns, cloudWatch)
        cloudWatchPub.OnPutMetricError.Add(fun exn' -> exn := exn')
        Publish.pubWith(cloudWatchPub)
        
        MetricsAgent.IncrementCountMetricBy("CountMetric", 1500L)
        MetricsAgent.RecordTimeSpanMetric("TimeMetric", TimeSpan.FromSeconds 1.5)
        
        Thread.Sleep(TimeSpan.FromSeconds 2.0) // give it time to push data to the publishers

        // metrics should now be aggregated at publisher, force the publisher to upload it
        Publish.stop()
        
        let exn = !exn
        exn         |> should be instanceOfType<AggregateException>
        (exn :?> AggregateException).InnerException.Message |> should equal "boom"

    [<Test>]
    member test.``metrics published across multiple publish intervals are aggregated into per minute metrics`` () =
        let req : PutMetricDataRequest ref = ref null
        let res = Task.FromResult <| PutMetricDataResponse()
        let ns  = "test"

        let cloudWatch =
            Mock<Amazon.CloudWatch.IAmazonCloudWatch>()
                .Setup(fun x -> <@ x.PutMetricDataAsync(any(), any()) @>)
                .Calls<PutMetricDataRequest * CancellationToken option>(fun (req', _) -> req := req'; res)
                .Create()

        let cloudWatchPub = new CloudWatchPublisher(ns, cloudWatch)
        Publish.pubWith(cloudWatchPub)

        MetricsAgent.IncrementCountMetricBy("CountMetric", 1500L)
        MetricsAgent.RecordTimeSpanMetric("TimeMetric", TimeSpan.FromSeconds 1.5)
        
        Thread.Sleep(TimeSpan.FromMilliseconds 1.0 + Publish.interval) // give it time to push data to the publishers

        MetricsAgent.IncrementCountMetricBy("CountMetric", 500L)
        MetricsAgent.RecordTimeSpanMetric("TimeMetric", TimeSpan.FromSeconds 0.5)
        
        Thread.Sleep(TimeSpan.FromMilliseconds 1.0 + Publish.interval) // give it time to push data to the publishers

        // force the data to be published
        // metrics should now be aggregated at publisher, force the publisher to upload it
        Publish.stop()

        let req = !req
        req.Namespace   |> should equal ns
        req.MetricData  |> should haveCount 2

        let countMetric = req.MetricData  |> Seq.find (fun datum -> datum.MetricName = "CountMetric")        
        countMetric.Unit                  |> should equal StandardUnit.Count
        countMetric.Dimensions.Count      |> should equal 1
        countMetric.Dimensions.[0].Name   |> should equal "Type"
        countMetric.Dimensions.[0].Value  |> should equal "Count"
        
        countMetric.StatisticValues.Sum          |> should equal 2000.0
        countMetric.StatisticValues.SampleCount  |> should equal 2.0
        countMetric.StatisticValues.Maximum      |> should equal 1500.0
        countMetric.StatisticValues.Minimum      |> should equal 500.0        

        let timeMetric  = req.MetricData |> Seq.find (fun datum -> datum.MetricName = "TimeMetric")
        timeMetric.Unit                  |> should equal StandardUnit.Milliseconds
        timeMetric.Dimensions.Count      |> should equal 1
        timeMetric.Dimensions.[0].Name   |> should equal "Type"
        timeMetric.Dimensions.[0].Value  |> should equal "TimeSpan"

        timeMetric.StatisticValues.Sum           |> should equal 2000.0
        timeMetric.StatisticValues.SampleCount   |> should equal 2.0        
        timeMetric.StatisticValues.Maximum       |> should equal 1500.0
        timeMetric.StatisticValues.Minimum       |> should equal 500.0