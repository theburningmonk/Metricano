namespace Metricano

open System
open System.Threading.Tasks

type MetricType =
    | TimeSpan = 1uy
    | Count    = 2uy

type Metric = 
    {
        Type                : MetricType
        Name                : string
        TimeStamp           : DateTime
        Unit                : string
        mutable Average     : double
        mutable Sum         : double
        mutable Max         : double
        mutable Min         : double
        mutable Count       : double
    }

[<Class>]
type MetricsAgent =
    static member RecordTimeSpanMetric   : string * TimeSpan -> unit
    static member IncrementCountMetric   : string -> unit
    static member IncrementCountMetricBy : string * int64 -> unit
    static member SetCountMetric         : string * int64 -> unit
    static member internal Flush         : unit -> Task<Metric[]>

type IMetricsPublisher =
    abstract member Publish : Metric[] -> Task