namespace Metricano.PostSharpAspects
{
    /// <summary>
    /// Enumerates the possible behaviors of the calling method after the calling method has returned.
    /// </summary>
    public enum TaskFlowBehaviour
    {
        Default,
        Continue,
        RethrowException,
        Return,

        /// <summary>
        /// Throws the exception contained in the Exception property. Available only for OnTaskFaulted(TaskExecutionArgs).
        /// </summary>
        ThrowException,
    }
}