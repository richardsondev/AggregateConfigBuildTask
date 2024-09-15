using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace AggregateConfigBuildTask
{
    /// <inheritdoc />
    public class TaskLogger : ITaskLogger
    {
        /// <summary>
        /// Gets the underlying TaskLoggingHelper.
        /// </summary>
        public TaskLoggingHelper Log { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskLogger"/> class.
        /// </summary>
        /// <param name="task">The task that will be used for logging.</param>
        public TaskLogger(TaskLoggingHelper task)
        {
            Log = task ?? throw new ArgumentNullException(nameof(task));
        }

        /// <inheritdoc />
        public void LogError(string message = null, params object[] messageArgs)
        {
            Log.LogError(message, messageArgs);
        }

        /// <inheritdoc />
        public void LogError(
            string subcategory = null,
            string errorCode = null,
            string helpKeyword = null,
            string file = null,
            int lineNumber = 0,
            int columnNumber = 0,
            int endLineNumber = 0,
            int endColumnNumber = 0,
            string message = null,
            params object[] messageArgs)
        {
            Log.LogError(
                subcategory,
                errorCode,
                helpKeyword,
                file,
                lineNumber,
                columnNumber,
                endLineNumber,
                endColumnNumber,
                message,
                messageArgs);
        }

        /// <inheritdoc />
        public void LogErrorFromException(Exception exception,
            bool showStackTrace = false,
            bool showDetail = false,
            string file = null)
        {
            Log.LogErrorFromException(exception, showStackTrace, showDetail, file);
        }

        /// <inheritdoc />
        public void LogWarning(string message = null, params object[] messageArgs)
        {
            Log.LogWarning(message, messageArgs);
        }

        /// <inheritdoc />
        public void LogWarning(
            string subcategory = null,
            string warningCode = null,
            string helpKeyword = null,
            string file = null,
            int lineNumber = 0,
            int columnNumber = 0,
            int endLineNumber = 0,
            int endColumnNumber = 0,
            string message = null,
            params object[] messageArgs)
        {
            Log.LogWarning(
                subcategory,
                warningCode,
                helpKeyword,
                file,
                lineNumber,
                columnNumber,
                endLineNumber,
                endColumnNumber,
                message,
                messageArgs);
        }

        /// <inheritdoc />
        public void LogMessage(MessageImportance importance = MessageImportance.Normal, string message = null, params object[] messageArgs)
        {
            Log.LogMessage(importance, message, messageArgs);
        }
    }
}
