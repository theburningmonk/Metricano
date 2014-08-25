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

type IMetricsAgent =
    abstract member RecordTimeSpanMetric   : string * TimeSpan -> unit
    abstract member IncrementCountMetric   : string -> unit
    abstract member IncrementCountMetricBy : string * int64 -> unit
    abstract member SetCountMetric         : string * int64 -> unit
    abstract member internal Flush         : unit -> Task<Metric[]>

[<Class>]
[<Sealed>]
type MetricsAgent =
    interface IMetricsAgent

    static member Default   : IMetricsAgent
    static member Create    : ?maxRawTimespans : int -> IMetricsAgent

type IMetricsPublisher =
    inherit IDisposable

    abstract member Publish : Metric[] -> Task