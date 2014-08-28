Metricano
=========

Agent-based F# library for collecting, aggregating and publishing metrics (e.g. to Amazon CloudWatch).

### Motivation ###

> To provide an easy and efficient way for you to collect and publish method/function level metrics, be it to Amazon CloudWatch, StackDriver or your custom dashboard.


### Getting Started ###

There are three concepts you need to remember with working with `Metricano`:

- the [Metric](https://github.com/theburningmonk/Metricano/blob/master/src/Metricano/Core.fsi#L32-L44) type represents a metric that have been collected on your behalf (e.g. execution time for the `foo` function) 
- a **MetricsAgent** (represented by the [IMetricsAgent](https://github.com/theburningmonk/Metricano/blob/master/src/Metricano/Core.fsi#L46-L51) interface) is responsible for tracking your metrics
- a **Publisher** (represented by the [IMetricsPublisher](https://github.com/theburningmonk/Metricano/blob/master/src/Metricano/Core.fsi#L61-L64) interface) is responsible for publishing your metrics, this is *what you have to implement* if you want to publish metrics to a custom dashboard for instance.

To start using `Metricano` you need to download at least one of the three Nuget packages available:

#### [Metricano](https://www.nuget.org/packages/Metricano/) ####

This is the main package where the [MetricsAgent](https://github.com/theburningmonk/Metricano/blob/master/src/Metricano/Core.fsi#L53-L59) static class is defined. Most of the time you should be using the [Default](https://github.com/theburningmonk/Metricano/blob/master/src/Metricano/Core.fsi#L58) metrics agent, which in the case of execution time metrics caps the number of data points per metric to 500.

If this limit is too low for you, you can [Create](https://github.com/theburningmonk/Metricano/blob/master/src/Metricano/Core.fsi#L59) a new metrics agent with a higher limit. Be ware though this *could lead to excessive memory usage* (and potentially cause `OutOfMemoryException`) if you try to record execution time for a large number of frequently executed methods. E.g. tracking every execution for methods that are executed millions of times a second is not going to end well.

Once you have downloaded the package, you can start recording metrics like this:

```csharp
using Metricano

//...
MetricsAgent.Default.IncrementCountMetric("NumberOfSocketAccepts");
MetricsAgent.Default.RecordTimeSpanMetric("SessionDuration", TimeSpan.FromMinutes(session.DurationMins));
```

Your collected metrics will be published to all configured metrics publishers **every second**. 

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

### TL;DR ###

![](http://i.imgur.com/HcPeU9o.gif)