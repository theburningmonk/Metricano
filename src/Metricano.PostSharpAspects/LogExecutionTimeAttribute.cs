using System;
using System.Diagnostics;
using System.Reflection;

using PostSharp.Aspects;
using PostSharp.Extensibility;

namespace Metricano.PostSharpAspects
{
    [Serializable]
    [DebuggerStepThrough]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
    [MulticastAttributeUsage(MulticastTargets.Method, PersistMetaData = true)]
    public sealed class LogExecutionTimeAttribute : BaseMetricAttribute
    {
        public override void OnEntry(MethodExecutionArgs args)
        {
            // start a stop watch when entering the method
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            args.MethodExecutionTag = stopwatch;

            base.OnEntry(args);
        }

        public override void OnExitSync(MethodExecutionArgs args)
        {
            var stopwatch = (Stopwatch)args.MethodExecutionTag;
            stopwatch.Stop();
            HandleElapsedTime(args.Method, stopwatch.Elapsed);
        }

        public override void OnTaskFinished(TaskExecutionArgs args)
        {
            var stopwatch = (Stopwatch)args.MethodExecutionTag;
            stopwatch.Stop();
            HandleElapsedTime(args.Method, stopwatch.Elapsed);
        }

        private void HandleElapsedTime(MethodBase method, TimeSpan elapsedTime)
        {
            PublishMetric(method, elapsedTime);
        }

        private void PublishMetric(MethodBase method, TimeSpan executionTime)
        {
            var metricName = GetMetricName(method.DeclaringType, method.Name, method.IsGenericMethod, method.GetGenericArguments());
            MetricsAgent.RecordTimeSpanMetric(metricName, executionTime);
        }
    }
}