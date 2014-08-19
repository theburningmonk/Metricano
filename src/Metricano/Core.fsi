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
        Namespace           : string
        mutable Average     : double
        mutable Sum         : double
        mutable Max         : double
        mutable Min         : double
        mutable Count       : double
    }

type MetricsAgent =
    new : string -> MetricsAgent
    
    member RecordTimeSpanMetric   : string * TimeSpan -> unit
    member IncrementCountMetric   : string -> unit
    member IncrementCountMetricBy : string * int64 -> unit
    member SetCountMetric         : string * int64 -> unit
    member Flush                  : unit -> Task<Metric[]>

type IMetricsPublisher =
    abstract member Publish : Metric[] -> unit