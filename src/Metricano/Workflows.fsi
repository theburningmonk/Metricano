namespace Metricano

[<AutoOpen>]
module Timed =     
    [<Sealed>]
    type TimeMetricBuilder = class end

    val timeMetric : string -> IMetricsAgent -> TimeMetricBuilder

[<AutoOpen>]
module Counted =     
    [<Sealed>]
    type CountMetricBuilder = class end

    val countMetric : string -> IMetricsAgent -> CountMetricBuilder