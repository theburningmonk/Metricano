using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using PostSharp.Aspects;
using PostSharp.Extensibility;

namespace Metricano.PostSharpAspects
{
    /// <summary>
    /// Base class for attributes that needs to publish metrics based on execution of a method
    /// </summary>
    [Serializable]
    [DebuggerStepThrough]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly,
                    AllowMultiple = true)]
    [MulticastAttributeUsage(MulticastTargets.Method, PersistMetaData = true)]
    public abstract class BaseMetricAttribute : OnAsyncMethodBoundaryAspect
    {
        /// <summary>
        /// If configured to publish metric then publish metric under this name. If not set
        /// then the method's full name will be used, e.g. MyApp.MyClass.MyMethod
        /// </summary>
        public string MetricName { get; set; }

        /// <summary>
        /// Whether or not a metric name should be generated
        /// </summary>
        private bool AutoGenerateMetricName { get; set; }

        public override void CompileTimeInitialize(MethodBase method, AspectInfo aspectInfo)
        {
            // initialize metric name if an override is not provided
            AutoGenerateMetricName = string.IsNullOrWhiteSpace(MetricName);

            base.CompileTimeInitialize(method, aspectInfo);
        }

        protected string GetMetricName(Type declaringType, string methodName, bool isGenericMethod, Type[] genericArguments)
        {
            if (!AutoGenerateMetricName)
            {
                return MetricName;
            }

            if (isGenericMethod)
            {
                return string.Format(
                    "{0}.{1}<{2}>",
                    declaringType.Name,
                    methodName,
                    genericArguments.Select(t => t.Name).ToCsv());
            }

            return string.Format("{0}.{1}", declaringType.Name, methodName);
        }
    }
}