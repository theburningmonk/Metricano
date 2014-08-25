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
    public sealed class CountExecutionAttribute : BaseMetricAttribute
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
            IncrementCountMetric(args.Method);
        }

        public override void OnTaskFinished(TaskExecutionArgs args)
        {
            IncrementCountMetric(args.Method);
        }

        private void IncrementCountMetric(MethodBase method)
        {
            var metricName = GetMetricName(method.DeclaringType, method.Name, method.IsGenericMethod, method.GetGenericArguments());
            MetricsAgent.Default.IncrementCountMetric(metricName);
        }
    }
}