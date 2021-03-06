﻿namespace Metricano.Tests

open System

open FsUnit
open NUnit.Framework
open Metricano

[<TestFixture>]
type ``MetricsAgent tests`` () =
    [<Test>]
    member test.``when two TimeSpan metrics are recorded, them should be aggregated`` () =
        MetricsAgent.Default.RecordTimeSpanMetric("TestA", TimeSpan.FromMilliseconds 1.0)
        MetricsAgent.Default.RecordTimeSpanMetric("TestA", TimeSpan.FromMilliseconds 3.0)

        let metrics = MetricsAgent.Default.Flush().Result
        metrics.Length      |> should equal 1

        let metric = metrics.[0]
        metric.Type         |> should equal MetricType.TimeSpan
        metric.Name         |> should equal "TestA"
        metric.Unit         |> should equal "Milliseconds"
        metric.SampleCount  |> should equal 2.0
        metric.Sum          |> should equal 4.0
        metric.Max          |> should equal 3.0
        metric.Min          |> should equal 1.0
        metric.Average      |> should equal 2.0

    [<Test>]
    member test.``when two Count metrics are recorded, they should be aggregated`` () =
        MetricsAgent.Default.IncrementCountMetric("TestA")
        MetricsAgent.Default.IncrementCountMetricBy("TestA", 3L)       

        let metrics = MetricsAgent.Default.Flush().Result
        metrics.Length      |> should equal 1

        let metric = metrics.[0]
        metric.Type         |> should equal MetricType.Count
        metric.Name         |> should equal "TestA"
        metric.Unit         |> should equal "Count"
        metric.SampleCount  |> should equal 2.0
        metric.Sum          |> should equal 4.0
        metric.Max          |> should equal 3.0
        metric.Min          |> should equal 1.0
        metric.Average      |> should equal 2.0

    [<Test>]
    member test.``when 10000 TimeSpan metrics are recorded, they should be capped by maxRawTimespan (default is 500)`` () =
        { 1..10000 } |> Seq.iter (fun _ -> MetricsAgent.Default.RecordTimeSpanMetric("TestA", TimeSpan.FromMilliseconds 1.0))

        let metrics = MetricsAgent.Default.Flush().Result
        metrics.Length      |> should equal 1

        let metric = metrics.[0]
        metric.Type         |> should equal MetricType.TimeSpan
        metric.Name         |> should equal "TestA"
        metric.Unit         |> should equal "Milliseconds"
        metric.SampleCount  |> should equal 500.0
        metric.Sum          |> should equal 500.0
        metric.Max          |> should equal 1.0
        metric.Min          |> should equal 1.0
        metric.Average      |> should equal 1.0

    [<Test>]
    member test.``when 10000 TimeSpan metrics are recorded with a custom metrics agent with maxRawTimespan at 10000 then they should all be tracked`` () =
        let metricsAgent = MetricsAgent.Create(10000)
        { 1..10000 } |> Seq.iter (fun _ -> metricsAgent.RecordTimeSpanMetric("TestA", TimeSpan.FromMilliseconds 1.0))

        let metrics = metricsAgent.Flush().Result
        metrics.Length      |> should equal 1

        let metric = metrics.[0]
        metric.Type         |> should equal MetricType.TimeSpan
        metric.Name         |> should equal "TestA"
        metric.Unit         |> should equal "Milliseconds"
        metric.SampleCount  |> should equal 10000.0
        metric.Sum          |> should equal 10000.0
        metric.Max          |> should equal 1.0
        metric.Min          |> should equal 1.0
        metric.Average      |> should equal 1.0

    [<Test>]
    member test.``when 10000 Count metrics are recorded, they should all be tracked correctly`` () =
        { 1..10000 } |> Seq.iter (fun _ -> MetricsAgent.Default.IncrementCountMetric("TestA"))

        let metrics = MetricsAgent.Default.Flush().Result
        metrics.Length      |> should equal 1
        
        let metric = metrics.[0]
        metric.Type         |> should equal MetricType.Count
        metric.Name         |> should equal "TestA"
        metric.Unit         |> should equal "Count"
        metric.SampleCount  |> should equal 10000.0
        metric.Sum          |> should equal 10000.0
        metric.Max          |> should equal 1.0
        metric.Min          |> should equal 1.0
        metric.Average      |> should equal 1.0

    [<Test>]
    member test.``when the count metric is set multiple times, it should have the value of the last set`` () =
        MetricsAgent.Default.SetCountMetric("TestA", 10L)
        
        let metrics = MetricsAgent.Default.Flush().Result
        metrics.Length      |> should equal 1
                
        let metric = metrics.[0]
        metric.Type         |> should equal MetricType.Count
        metric.Name         |> should equal "TestA"
        metric.Unit         |> should equal "Count"
        metric.SampleCount  |> should equal 1.0
        metric.Sum          |> should equal 10.0
        metric.Max          |> should equal 10.0
        metric.Min          |> should equal 10.0
        metric.Average      |> should equal 10.0

        MetricsAgent.Default.SetCountMetric("TestA", 20L)

        let metrics = MetricsAgent.Default.Flush().Result
        metrics.Length      |> should equal 1
        
        let metric = metrics.[0]
        metric.Type         |> should equal MetricType.Count
        metric.Name         |> should equal "TestA"
        metric.Unit         |> should equal "Count"
        metric.SampleCount  |> should equal 1.0
        metric.Sum          |> should equal 20.0
        metric.Max          |> should equal 20.0
        metric.Min          |> should equal 20.0
        metric.Average      |> should equal 20.0

    [<Test>]
    member test.``after flushing, all the metrics are cleared afterwards`` () =
        MetricsAgent.Default.RecordTimeSpanMetric("TestA", TimeSpan.FromMilliseconds 1.0)
        MetricsAgent.Default.RecordTimeSpanMetric("TestB", TimeSpan.FromMilliseconds 1.0)
        MetricsAgent.Default.IncrementCountMetric("TestA")
        MetricsAgent.Default.IncrementCountMetric("TestB")

        let metrics = MetricsAgent.Default.Flush().Result
        metrics.Length      |> should equal 4

        let metrics = MetricsAgent.Default.Flush().Result
        metrics.Length      |> should equal 0