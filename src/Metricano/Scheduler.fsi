namespace Metricano

open System

[<Class>]
[<Sealed>]
type Publish =
    /// How frequently are collected metrics published to the configured publishers
    static member Interval     : TimeSpan

    /// Publish metrics from the Default IMetricsAgent with the specified publisher
    static member With         : IMetricsPublisher -> unit

    /// Publish metrics from the specified IMetricsAgent with the specified publisher
    static member With         : IMetricsAgent * IMetricsPublisher -> unit

    /// Flush any collected metrics and stop doing any more publishing
    static member Stop         : unit -> unit