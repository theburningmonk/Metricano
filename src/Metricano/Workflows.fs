namespace Metricano

open System.Diagnostics

[<AutoOpen>]
module Timed = 
    type TimedExecution<'a> = (unit -> 'a)
    let runExecution (exec : TimedExecution<'a>) = exec()

    [<Sealed>]
    type TimeMetricBuilder (name, metricsAgent : IMetricsAgent) =
        let sw = new Stopwatch()

        member x.Return value = 
            sw.Stop()
            metricsAgent.RecordTimeSpanMetric(name, sw.Elapsed)
            fun() -> value

        member x.ReturnFrom (exec : TimedExecution<'a>) = exec

        member x.Zero () =
            fun () -> ()

        member x.Delay f =
            fun () -> 
                sw.Start()
                runExecution(f())

    let timeMetric name metricsAgent = new TimeMetricBuilder(name, metricsAgent)
    
[<AutoOpen>]
module Counted =
    type CountedExecution<'a> = (unit ->'a)
    let runExecution (exec : CountedExecution<'a>) = exec()

    [<Sealed>]
    type CountMetricBuilder (name, metricsAgent : IMetricsAgent) =
        member x.Bind (p, rest) =
            metricsAgent.IncrementCountMetric(name)
            rest p

        member x.Return (value) =
            fun () -> value

        member x.Zero () =
            fun () -> ()

    let countMetric name metricsAgent = new CountMetricBuilder(name, metricsAgent)