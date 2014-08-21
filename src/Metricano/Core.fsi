namespace Metricano

open System
open System.Threading.Tasks

type MetricType =
    | TimeSpan = 1uy
    | Count    = 2uy

type CountMetric =
    {
        Name        : string
        TimeStamp   : DateTime
        Sum         : double
        Average     : double
        Max         : double
        Min         : double
        Count       : int64
    }

type TimeSpanMetric =
    {
        Name        : string
        TimeStamp   : DateTime
        Sum         : TimeSpan
        Average     : TimeSpan
        Max         : TimeSpan
        Min         : TimeSpan
        Raw         : TimeSpan[]
    }

type Metric = 
    | Count     of CountMetric
    | TimeSpan  of TimeSpanMetric

    member Name         : string
    member TimeStamp    : DateTime
    member Type         : MetricType
    member Unit         : string
    member Sum          : double
    member Average      : double
    member Max          : double
    member Min          : double
    member SampleCount  : int64

[<Class>]
type MetricsAgent =
    static member RecordTimeSpanMetric   : string * TimeSpan -> unit
    static member IncrementCountMetric   : string -> unit
    static member IncrementCountMetricBy : string * int64 -> unit
    static member SetCountMetric         : string * int64 -> unit
    static member internal Flush         : unit -> Task<Metric[]>

type IMetricsPublisher =
    abstract member Publish : Metric[] -> Task