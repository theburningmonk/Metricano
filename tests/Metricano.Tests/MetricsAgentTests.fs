namespace Metricano.Tests

open System
open FsUnit
open NUnit.Framework
open Metricano

[<TestFixture>]
type ``MetricsAgent tests`` () =
    [<Test>]
    member test.``when two TimeSpan metrics are recorded, them should be aggregated`` () =
        MetricsAgent.RecordTimeSpanMetric("TestA", TimeSpan.FromMilliseconds 1.0)
        MetricsAgent.RecordTimeSpanMetric("TestA", TimeSpan.FromMilliseconds 3.0)

        let metrics = MetricsAgent.Flush().Result
        metrics.Length      |> should equal 1

        let metric = metrics.[0]
        metric.Type         |> should equal MetricType.TimeSpan
        metric.Name         |> should equal "TestA"
        metric.Unit         |> should equal "Milliseconds"
        metric.Count        |> should equal 2.0
        metric.Sum          |> should equal 4.0
        metric.Max          |> should equal 3.0
        metric.Min          |> should equal 1.0
        metric.Average      |> should equal 2.0

    [<Test>]
    member test.``when two Count metrics are recorded, they should be aggregated`` () =
        MetricsAgent.IncrementCountMetric("TestA")
        MetricsAgent.IncrementCountMetricBy("TestA", 3L)       

        let metrics = MetricsAgent.Flush().Result
        metrics.Length      |> should equal 1

        let metric = metrics.[0]
        metric.Type         |> should equal MetricType.Count
        metric.Name         |> should equal "TestA"
        metric.Unit         |> should equal "Count"
        metric.Count        |> should equal 2.0
        metric.Sum          |> should equal 4.0
        metric.Max          |> should equal 3.0
        metric.Min          |> should equal 1.0
        metric.Average      |> should equal 2.0

    [<Test>]
    member test.``when 10000 TimeSpan metrics are recorded, they should all be tracked correctly`` () =
        { 1..10000 } |> Seq.iter (fun _ -> MetricsAgent.RecordTimeSpanMetric("TestA", TimeSpan.FromMilliseconds 1.0))

        let metrics = MetricsAgent.Flush().Result
        metrics.Length      |> should equal 1

        let metric = metrics.[0]
        metric.Type         |> should equal MetricType.TimeSpan
        metric.Name         |> should equal "TestA"
        metric.Unit         |> should equal "Milliseconds"
        metric.Count        |> should equal 10000.0
        metric.Sum          |> should equal 10000.0
        metric.Max          |> should equal 1.0
        metric.Min          |> should equal 1.0
        metric.Average      |> should equal 1.0

    [<Test>]
    member test.``when 10000 Count metrics are recorded, they should all be tracked correctly`` () =
        { 1..10000 } |> Seq.iter (fun _ -> MetricsAgent.IncrementCountMetric("TestA"))

        let metrics = MetricsAgent.Flush().Result
        metrics.Length      |> should equal 1
        
        let metric = metrics.[0]
        metric.Type         |> should equal MetricType.Count
        metric.Name         |> should equal "TestA"
        metric.Unit         |> should equal "Count"
        metric.Count        |> should equal 10000.0
        metric.Sum          |> should equal 10000.0
        metric.Max          |> should equal 1.0
        metric.Min          |> should equal 1.0
        metric.Average      |> should equal 1.0

    [<Test>]
    member test.``when the count metric is set multiple times, it should have the value of the last set`` () =
        MetricsAgent.SetCountMetric("TestA", 10L)
        
        let metrics = MetricsAgent.Flush().Result
        metrics.Length      |> should equal 1
                
        let metric = metrics.[0]
        metric.Type         |> should equal MetricType.Count
        metric.Name         |> should equal "TestA"
        metric.Unit         |> should equal "Count"
        metric.Count        |> should equal 1.0
        metric.Sum          |> should equal 10.0
        metric.Max          |> should equal 10.0
        metric.Min          |> should equal 10.0
        metric.Average      |> should equal 10.0

        MetricsAgent.SetCountMetric("TestA", 20L)

        let metrics = MetricsAgent.Flush().Result
        metrics.Length      |> should equal 1
        
        let metric = metrics.[0]
        metric.Type         |> should equal MetricType.Count
        metric.Name         |> should equal "TestA"
        metric.Unit         |> should equal "Count"
        metric.Count        |> should equal 1.0
        metric.Sum          |> should equal 20.0
        metric.Max          |> should equal 20.0
        metric.Min          |> should equal 20.0
        metric.Average      |> should equal 20.0

    [<Test>]
    member test.``after flushing, all the metrics are cleared afterwards`` () =
        MetricsAgent.RecordTimeSpanMetric("TestA", TimeSpan.FromMilliseconds 1.0)
        MetricsAgent.RecordTimeSpanMetric("TestB", TimeSpan.FromMilliseconds 1.0)
        MetricsAgent.IncrementCountMetric("TestA")
        MetricsAgent.IncrementCountMetric("TestB")

        let metrics = MetricsAgent.Flush().Result
        metrics.Length      |> should equal 4

        let metrics = MetricsAgent.Flush().Result
        metrics.Length      |> should equal 0