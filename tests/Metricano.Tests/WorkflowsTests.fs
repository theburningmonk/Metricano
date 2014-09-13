namespace Metricano.Tests

open System.Threading

open FsUnit
open NUnit.Framework
open Metricano

[<TestFixture>]
type ``TimedWorkflow tests`` () =
    [<Test>]
    member test.``timeMetric workflow should capture execution time of whole workflow`` () =
        let timed = timeMetric "TestA" MetricsAgent.Default {
            Thread.Sleep(10)
            Thread.Sleep(10)
            return ()
        }

        do timed()

        let metrics = MetricsAgent.Default.Flush().Result
        metrics     |> should haveLength 1
        
        let metric  = metrics.[0]
        metric.Type         |> should equal MetricType.TimeSpan
        metric.Name         |> should equal "TestA"
        metric.Unit         |> should equal "Milliseconds"
        metric.SampleCount  |> should equal 1.0
        metric.Sum          |> should (equalWithin 1.0) 20.0
        metric.Max          |> should (equalWithin 1.0) 20.0
        metric.Min          |> should (equalWithin 1.0) 20.0
        metric.Average      |> should (equalWithin 1.0) 20.0
        
    [<Test>]
    member test.``nested timeMetric workflow should capture execution time independently`` () =
        let nested = timeMetric "Nested" MetricsAgent.Default {
            Thread.Sleep(10) // pretend doing some IO
            return()
        }

        let parent = timeMetric "Parent" MetricsAgent.Default {
            do nested()
            Thread.Sleep(10) // pretend doing some more IO
            return()
        }

        do parent()

        let metrics = MetricsAgent.Default.Flush().Result
        metrics     |> should haveLength 2
        
        let metric  = metrics.[0]
        metric.Type         |> should equal MetricType.TimeSpan
        metric.Name         |> should equal "Nested"
        metric.Unit         |> should equal "Milliseconds"
        metric.SampleCount  |> should equal 1.0
        metric.Sum          |> should (equalWithin 1.0) 10.0
        metric.Max          |> should (equalWithin 1.0) 10.0
        metric.Min          |> should (equalWithin 1.0) 10.0
        metric.Average      |> should (equalWithin 1.0) 10.0
        
        let metric  = metrics.[1]
        metric.Type         |> should equal MetricType.TimeSpan
        metric.Name         |> should equal "Parent"
        metric.Unit         |> should equal "Milliseconds"
        metric.SampleCount  |> should equal 1.0
        metric.Sum          |> should (equalWithin 1.0) 20.0
        metric.Max          |> should (equalWithin 1.0) 20.0
        metric.Min          |> should (equalWithin 1.0) 20.0
        metric.Average      |> should (equalWithin 1.0) 20.0

[<TestFixture>]
type ``CountedWorkflow tests`` () =
    [<Test>]
    member test.``countMetric workflow should capture do! and let! in the workflow`` () =
        let counted = countMetric "TestA" MetricsAgent.Default {
            do! ()
            let! _ = 42
            return ()
        }

        do counted()

        let metrics = MetricsAgent.Default.Flush().Result
        metrics     |> should haveLength 1
        
        let metric  = metrics.[0]
        metric.Type         |> should equal MetricType.Count
        metric.Name         |> should equal "TestA"
        metric.Unit         |> should equal "Count"
        metric.SampleCount  |> should equal 2.0
        metric.Sum          |> should equal 2.0
        metric.Max          |> should equal 1.0
        metric.Min          |> should equal 1.0
        metric.Average      |> should equal 1.0

    [<Test>]
    member test.``empty countMetric workflow should not capture any metrics`` () =
        let notCounted = countMetric "TestA" MetricsAgent.Default {
            return ()
        }

        do notCounted()

        let metrics = MetricsAgent.Default.Flush().Result
        metrics     |> should haveLength 0
        
    [<Test>]
    member test.``nested countMetric workflow should capture counts independently`` () =
        let nested = countMetric "Nested" MetricsAgent.Default {
            do! ()
        }

        let parent = countMetric "Parent" MetricsAgent.Default {
            let! _ = 42
            do! ()

            do nested()
        }

        do parent()

        let metrics = MetricsAgent.Default.Flush().Result
        metrics     |> should haveLength 2
        
        let metric  = metrics.[0]
        metric.Type         |> should equal MetricType.Count
        metric.Name         |> should equal "Nested"
        metric.Unit         |> should equal "Count"
        metric.SampleCount  |> should equal 1.0
        metric.Sum          |> should equal 1.0
        metric.Max          |> should equal 1.0
        metric.Min          |> should equal 1.0
        metric.Average      |> should equal 1.0
        
        let metric  = metrics.[1]
        metric.Type         |> should equal MetricType.Count
        metric.Name         |> should equal "Parent"
        metric.Unit         |> should equal "Count"
        metric.SampleCount  |> should equal 2.0
        metric.Sum          |> should equal 2.0
        metric.Max          |> should equal 1.0
        metric.Min          |> should equal 1.0
        metric.Average      |> should equal 1.0