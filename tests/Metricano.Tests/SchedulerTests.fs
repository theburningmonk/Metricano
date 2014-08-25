namespace Metricano.Tests

open System
open System.Threading
open System.Threading.Tasks

open FsUnit
open NUnit.Framework
open Metricano

[<TestFixture>]
type ``Publish tests`` () =
    [<Test>]
    member test.``when publishing is stopped, all metrics are pushed to publishers`` () =
        let metrics : Metric[] ref = ref [||]
        let publisher = { new IMetricsPublisher with
                            member this.Publish metrics' = 
                                match !metrics with
                                | null | [||] 
                                    -> metrics := metrics'
                                       Task.Delay(1)
                                | _ -> Task.Delay(1)
                            member this.Dispose() = () }
        let metricsAgent = MetricsAgent.Create()
        Publish.With(metricsAgent, publisher)

        metricsAgent.SetCountMetric("CountMetricA", 1500L)
        metricsAgent.IncrementCountMetricBy("CountMetricA", 500L)
        metricsAgent.SetCountMetric("CountMetricB", 2000L)
        metricsAgent.RecordTimeSpanMetric("TimeMetricA", TimeSpan.FromMinutes 1.5)
        metricsAgent.RecordTimeSpanMetric("TimeMetricA", TimeSpan.FromMinutes 0.5)
        metricsAgent.RecordTimeSpanMetric("TimeMetricB", TimeSpan.FromMinutes 2.0)

        Publish.Stop()

        !metrics    |> should haveLength 4
        !metrics 
        |> Array.exists (fun m ->
            m.Name        = "CountMetricA" &&
            m.Type        = MetricType.Count &&
            m.Unit        = "Count" &&
            m.SampleCount = 2L &&
            m.Sum         = 2000.0 &&
            m.Average     = 1000.0 &&
            m.Max         = 1500.0 &&
            m.Min         = 500.0)
        |> should equal true

        !metrics 
        |> Array.exists (fun m ->
            m.Name        = "CountMetricB" &&
            m.Type        = MetricType.Count &&
            m.Unit        = "Count" &&
            m.SampleCount = 1L &&
            m.Sum         = 2000.0 &&
            m.Average     = 2000.0 &&
            m.Max         = 2000.0 &&
            m.Min         = 2000.0)
        |> should equal true

        !metrics 
        |> Array.exists (fun m ->
            m.Name        = "TimeMetricA" &&
            m.Type        = MetricType.TimeSpan &&
            m.Unit        = "Milliseconds" &&
            m.SampleCount = 2L &&
            m.Sum         = TimeSpan.FromMinutes(2.0).TotalMilliseconds &&
            m.Average     = TimeSpan.FromMinutes(1.0).TotalMilliseconds &&
            m.Max         = TimeSpan.FromMinutes(1.5).TotalMilliseconds &&
            m.Min         = TimeSpan.FromMinutes(0.5).TotalMilliseconds)
        |> should equal true

        !metrics 
        |> Array.exists (fun m ->
            m.Name        = "TimeMetricB" &&
            m.Type        = MetricType.TimeSpan &&
            m.Unit        = "Milliseconds" &&
            m.SampleCount = 1L &&
            m.Sum         = TimeSpan.FromMinutes(2.0).TotalMilliseconds &&
            m.Average     = TimeSpan.FromMinutes(2.0).TotalMilliseconds &&
            m.Max         = TimeSpan.FromMinutes(2.0).TotalMilliseconds &&
            m.Min         = TimeSpan.FromMinutes(2.0).TotalMilliseconds)
        |> should equal true