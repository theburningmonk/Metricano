using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

using PostSharp.Aspects;

namespace Metricano.PostSharpAspects
{
    /// <summary>
    /// An async method friendly variant of the OnMethodBoundaryAspect
    /// </summary>
    [Serializable]
    [DebuggerStepThrough]
    public abstract class OnAsyncMethodBoundaryAspect : OnMethodBoundaryAspect
    {
        /// <summary>
        /// Whether or not the method is 'async', e.g. whether or not it returns a task
        /// </summary>
        protected bool IsAsync { get; set; }

        public override void CompileTimeInitialize(MethodBase method, AspectInfo aspectInfo)
        {
            var methodInfo = method as MethodInfo;
            if (methodInfo == null)
            {
                throw new Exception("MethodInfo is null");
            }

            IsAsync = typeof(Task).IsAssignableFrom(methodInfo.ReturnType);

            base.CompileTimeInitialize(method, aspectInfo);
        }

        public sealed override void OnExit(MethodExecutionArgs args)
        {
            if (!IsAsync)
            {
                OnExitSync(args);
                return;
            }

            var task = (dynamic)args.ReturnValue;

            // hook up continuations for the returned task
            args.ReturnValue = GetContinuation(args, task);
        }

        public virtual void OnExitSync(MethodExecutionArgs args)
        {
        }

        /// <summary>
        /// Handler for when the preceding task returned by the method has finished, regardless whether or
        /// not the task faulted or ran to completion.
        /// </summary>
        public virtual void OnTaskFinished(TaskExecutionArgs args)
        {
        }

        /// <summary>
        /// Handler for when the preceding task returned by the method has faulted.
        /// </summary>
        public virtual void OnTaskFaulted(TaskExecutionArgs args)
        {
        }

        /// <summary>
        /// Handler for when the preceding task returned by the method has run to completion.
        /// </summary>
        public virtual void OnTaskCompletion(TaskExecutionArgs args)
        {
        }

        private async Task<TResult> GetContinuation<TResult>(MethodExecutionArgs args, Task<TResult> task)
        {
            return await task.ContinueWith(
                t => Continuation(t, args),
                TaskContinuationOptions.ExecuteSynchronously);
        }

        private async Task GetContinuation(MethodExecutionArgs args, Task task)
        {
            await task.ContinueWith(
                t => Continuation(t, args),
                TaskContinuationOptions.ExecuteSynchronously);
        }

        private TResult Continuation<TResult>(Task<TResult> precedingTask, MethodExecutionArgs args)
        {
            var taskArgs = new TaskExecutionArgs(precedingTask, args);

            if (precedingTask.IsCompleted && !precedingTask.IsFaulted)
            {
                taskArgs.ReturnValue = precedingTask.Result;
            }

            HandleTaskExecution(precedingTask, taskArgs);

            return (TResult)taskArgs.ReturnValue;
        }

        private void Continuation(Task precedingTask, MethodExecutionArgs args)
        {
            var taskArgs = new TaskExecutionArgs(precedingTask, args);

            HandleTaskExecution(precedingTask, taskArgs);
        }

        private void HandleTaskExecution(Task precedingTask, TaskExecutionArgs taskArgs)
        {
            try
            {
                if (precedingTask.IsFaulted)
                {
                    taskArgs.FlowBehavior = TaskFlowBehaviour.Default;
                    OnTaskFaulted(taskArgs);

                    switch (taskArgs.FlowBehavior)
                    {
                        case TaskFlowBehaviour.ThrowException:
                            // Throw given exception in preserving stack trace.
                            throw new AggregateException(taskArgs.Exception);
                        case TaskFlowBehaviour.Default:
                        case TaskFlowBehaviour.RethrowException:
                            // Rethrow exception.
                            precedingTask.Wait();
                            break;
                        case TaskFlowBehaviour.Continue:
                        case TaskFlowBehaviour.Return:
                            // Swallow exception and continue with execution.
                            break;
                    }
                }
                else if (precedingTask.IsCompleted)
                {
                    taskArgs.FlowBehavior = TaskFlowBehaviour.Default;
                    OnTaskCompletion(taskArgs);
                }
            }
            finally
            {
                taskArgs.FlowBehavior = TaskFlowBehaviour.Default;
                OnTaskFinished(taskArgs);
            }
        }
    }
}