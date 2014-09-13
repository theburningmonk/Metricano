Metricano
=========

Agent-based F# library for collecting, aggregating and publishing metrics (e.g. to Amazon CloudWatch).

### Motivation ###

> To provide an easy and efficient way for you to collect and publish method/function level metrics, be it to Amazon CloudWatch, StackDriver or your custom dashboard.


### TL;DR ###

<a href="#"><img src="http://i.imgur.com/HcPeU9o.gif" align="middle" height="300" width="350" ></a>

Record metrics with the `MetricsAgent` manually or using the AOP aspects from `Metricano.PostSharpAspects`. `MetricsAgent` will aggregate them into second-by-second summaries, and push these summaries to all publishers you have specified - `Metricano.CloudWatch` provides a publisher for publishing to `Amazon CloudWatch` service.

### Getting Started ###

There are three concepts you need to remember with working with `Metricano`:

- the [Metric](https://github.com/theburningmonk/Metricano/blob/master/src/Metricano/Core.fsi#L32-L44) type represents a metric that have been collected on your behalf (e.g. execution time for the `foo` function) 
- a **MetricsAgent** (represented by the [IMetricsAgent](https://github.com/theburningmonk/Metricano/blob/master/src/Metricano/Core.fsi#L46-L51) interface) is responsible for tracking your metrics
- a **Publisher** (represented by the [IMetricsPublisher](https://github.com/theburningmonk/Metricano/blob/master/src/Metricano/Core.fsi#L61-L64) interface) is responsible for publishing your metrics, this is *what you have to implement* if you want to publish metrics to a custom dashboard for instance.

To start using `Metricano` there are three separate Nuget packages available:

#### [Metricano](https://www.nuget.org/packages/Metricano/) ####

This is the main package where the [MetricsAgent](https://github.com/theburningmonk/Metricano/blob/master/src/Metricano/Core.fsi#L53-L59) static class is defined. Most of the time you should be using the [Default](https://github.com/theburningmonk/Metricano/blob/master/src/Metricano/Core.fsi#L58) metrics agent, which in the case of execution time metrics caps the number of data points per metric to 500.

If this limit is too low for you, you can [Create](https://github.com/theburningmonk/Metricano/blob/master/src/Metricano/Core.fsi#L59) a new metrics agent with a higher limit. Be ware though this *could lead to excessive memory usage* (and potentially cause `OutOfMemoryException`) if you try to record execution time for a large number of frequently executed methods. E.g. tracking every execution for methods that are executed millions of times a second is not going to end well.

Once you have downloaded the package, you can start recording metrics like this:

In C#..
```csharp
using Metricano

// you can track track metrics manually
MetricsAgent.Default.IncrementCountMetric("NumberOfSocketAccepts");
MetricsAgent.Default.RecordTimeSpanMetric("SessionDuration", TimeSpan.FromMinutes(session.DurationMins));

// but it's better to use the CountExecutionTime and LogExecutionTime attributes 
// from the Metricano.PostSharpAspects nuget package (see below)
```

In F#, in addition to tracking metrics manually you can also use the built-in `timeMetric` and `countMetric` workflows..   
```fsharp
open Metricano

// the timeMetric workflow captures time taken to execute the code in the workflow  
let timed = timeMetric "MyMetric" MetricsAgent.Default {
	Thread.Sleep(10) // pretend to do some IO
	return()
}

do timed() // this will record a time metric of 10ms against the metric "MyMetric"

let counted = countMetric "MyOtherMetric" MetricsAgent.Default {
	do! () 		// this increments the count by 1
	let! _ = 42 // this increments the count by 1 again
	return ()
}

do counted() // this will record the two datapoints above against the metric "MyOtherMetric"

// both timeMetric and countMetric workflows can be nested too
```

Your collected metrics will be published to all configured metrics publishers **every second**.

The examples you see here all use the **default** agent - `MetricsAgent.Default` - but if you require different pipelines of collecting and publishing metrics, you can also create instances of `IMetricsAgent` using the static method `MetricsAgent.Create`.
For instance, if you want to record and publish 95 percentile execution time metrics (see the `Metrics.CustomPublisherCs` project under `examples`) for a critical component to a live by-the-second dashboard; whilst all other application metrics are published to a less frequently-updated dashboard such as `Amazon CloudWatch`.

#### [Metricano.CloudWatch](https://www.nuget.org/packages/Metricano.CloudWatch/) ####

This package defines the metrics publisher for [Amazon CloudWatch](http://aws.amazon.com/cloudwatch/).

To publish metrics with the `CloudWatchPublisher` simply tell `Metricano` that you want to publish metrics [With](https://github.com/theburningmonk/Metricano/blob/master/src/Metricano/Scheduler.fsi#L12) an instance of `CloudWatchPublisher`:

```csharp
using Metricano
using Metricano.Publisher

//...
var publisher = new CloudWatchPublisher(
		                "MetricanoDemo", 
		                "YOUR_AWS_KEY_HERE", 
		                "YOUR_AWS_SECRET_HERE", 
		                RegionEndpoint.USEast1)
Publish.With(publisher);
```

Once published you'll be able to visualize your metrics in the CloudWatch console.

![](http://i.imgur.com/QviAJ90.png)

#### [Metricano.PostSharpAspects](https://www.nuget.org/packages/Metricano.PostSharpAspects/) ####

This package contains two PostSharp aspects:
- CountExecution
- LogExecutionTime

for tracking the execution count or execution time of your methods and they work for static, private, public, synchronous and asynchronous methods.

Both can be applied at method, class (which multicasts to all methods), or even assembly level. For example, if you want to track the execution count and time for every method in a class, then simply apply the attribute [at the class level](https://github.com/theburningmonk/Metricano/blob/master/examples/Metricano.PostSharpAspects.ExampleCs/Program.cs#L14-L15):

```csharp
[CountExecution]    // multi-cast to all methods, public & private
[LogExecutionTime]  // multi-cast to all methods, public & private
public class Program
{
	public static void Main(string[] args)
    {
        var prog = new Program();
        Publish.With(new CloudWatchPublisher(
                "MetricanoDemo",
                "YOUR_AWS_KEY_HERE",
                "YOUR_AWS_SECRET_HERE", 
                RegionEndpoint.USEast1));
	    //...
    }
}
```

### Resources ###

- Download [Metricano](https://www.nuget.org/packages/Metricano/), [Metricano.CloudWatch](https://www.nuget.org/packages/Metricano.CloudWatch/) and [Metricano.PostSharpAspects](https://www.nuget.org/packages/Metricano.PostSharpAspects/)
- [Bug Tracker](https://github.com/theburningmonk/Metricano/issues)
- Follow [@theburningmonk](https://twitter.com/theburningmonk) on Twitter for updates
- Watch this [webinar](http://vimeo.com/39197501) on performance monitoring with AOP and CloudWatch, or see slides [here](http://www.slideshare.net/theburningmonk/performance-monitoring-with-aop-and-amazon-cloudwatch) 