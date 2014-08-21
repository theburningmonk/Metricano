using System;
using System.Reflection;
using System.Threading.Tasks;

using PostSharp.Aspects;

namespace Metricano.PostSharpAspects
{
    /// <summary>
    /// Arguments of advices of aspects of type <see cref="T:OnAsyncMethodBoundaryAspect"/>.
    /// </summary>
    public sealed class TaskExecutionArgs
    {
        public TaskExecutionArgs(Task precedingTask, MethodExecutionArgs args)
        {
            Method = args.Method;
            Arguments = args.Arguments;

            Exception = precedingTask.Exception;
            FlowBehavior = TaskFlowBehaviour.Default;
            MethodExecutionTag = args.MethodExecutionTag;
        }

        /// <summary>
        /// Gets the method being executed.
        /// </summary>
        public MethodBase Method { get; set; }

        /// <summary>
        /// Gets the arguments with which the method has been invoked.
        /// </summary>
        public Arguments Arguments { get; set; }

        /// <summary>
        /// Gets or sets the method return value.
        /// </summary>
        public object ReturnValue { get; set; }

        /// <summary>
        /// Gets the exception currently flying.
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Determines the control flow of the target method once the advice is exited.
        /// </summary>
        public TaskFlowBehaviour FlowBehavior { get; set; }

        /// <summary>
        /// User-defined state information whose lifetime is linked to the current method execution. 
        /// Aspects derived from <see cref="T:OnAsyncMethodBoundaryAspect"/> should use this property to save 
        /// state information between different events.
        /// </summary>
        public object MethodExecutionTag { get; set; }
    }
}