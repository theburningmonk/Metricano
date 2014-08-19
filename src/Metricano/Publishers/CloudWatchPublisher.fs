namespace Metricano.Publisher

open Metricano

type CloudWatchPublisher () =    
    interface IMetricsPublisher with
        member this.Publish (metrics) = ()