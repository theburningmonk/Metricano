namespace Metricano

[<AutoOpen>]
module Timed =
    type TimedExecution<'a> = (unit -> 'a)

    [<Sealed>]
    type TimeMetricBuilder = 
        member Return     : 'a -> TimedExecution<'a>
        member ReturnFrom : TimedExecution<'a> -> TimedExecution<'a>
        member Zero       : unit -> TimedExecution<unit>
        member Delay      : (unit -> TimedExecution<'a>) -> TimedExecution<'a>

    val timeMetric : string -> IMetricsAgent -> TimeMetricBuilder

[<AutoOpen>]
module Counted =
    type CountedExecution<'a> = (unit ->'a)

    [<Sealed>]
    type CountMetricBuilder =
        member Bind   : 'a * ('a -> 'b) -> 'b
        member Return : 'a -> CountedExecution<'a>
        member Zero   : unit -> CountedExecution<unit>

    val countMetric : string -> IMetricsAgent -> CountMetricBuilder