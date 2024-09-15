using System;

using Microsoft.Build.Framework;

namespace AggregateConfigBuildTask
{
    /// <summary>
    /// Interface for task logging helper methods.
    /// </summary>
    public interface ITaskLogger
    {
        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        /// <param name="messageArgs">Optional arguments for formatting the message.</param>
        void LogError(string message = null, params object[] messageArgs);

        /// <summary>
        /// Logs an error message with additional parameters.
        /// </summary>
        /// <param name="subcategory">The subcategory of the error.</param>
        /// <param name="errorCode">The error code.</param>
        /// <param name="helpKeyword">The help keyword associated with the error.</param>
        /// <param name="file">The file in which the error occurred.</param>
        /// <param name="lineNumber">The line number where the error occurred.</param>
        /// <param name="columnNumber">The column number where the error occurred.</param>
        /// <param name="endLineNumber">The end line number of the error.</param>
        /// <param name="endColumnNumber">The end column number of the error.</param>
        /// <param name="message">The error message to log.</param>
        /// <param name="messageArgs">Optional arguments for formatting the message.</param>
        void LogError(
            string subcategory = null,
            string errorCode = null,
            string helpKeyword = null,
            string file = null,
            int lineNumber = 0,
            int columnNumber = 0,
            int endLineNumber = 0,
            int endColumnNumber = 0,
            string message = null,
            params object[] messageArgs);

        /// <summary>
        /// Logs an error message from an exception.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="showStackTrace">Indicates whether to show the stack trace.</param>
        /// <param name="showDetail">Indicates whether to show detailed information.</param>
        /// <param name="file">The file where the exception occurred.</param>
        void LogErrorFromException(Exception exception,
            bool showStackTrace = false,
            bool showDetail = false,
            string file = null);

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The warning message to log.</param>
        /// <param name="messageArgs">Optional arguments for formatting the message.</param>
        void LogWarning(string message = null, params object[] messageArgs);

        /// <summary>
        /// Logs a warning message with additional parameters.
        /// </summary>
        /// <param name="subcategory">The subcategory of the warning.</param>
        /// <param name="warningCode">The warning code.</param>
        /// <param name="helpKeyword">The help keyword associated with the warning.</param>
        /// <param name="file">The file in which the warning occurred.</param>
        /// <param name="lineNumber">The line number where the warning occurred.</param>
        /// <param name="columnNumber">The column number where the warning occurred.</param>
        /// <param name="endLineNumber">The end line number of the warning.</param>
        /// <param name="endColumnNumber">The end column number of the warning.</param>
        /// <param name="message">The warning message to log.</param>
        /// <param name="messageArgs">Optional arguments for formatting the message.</param>
        void LogWarning(
            string subcategory = null,
            string warningCode = null,
            string helpKeyword = null,
            string file = null,
            int lineNumber = 0,
            int columnNumber = 0,
            int endLineNumber = 0,
            int endColumnNumber = 0,
            string message = null,
            params object[] messageArgs);

        /// <summary>
        /// Logs a message with specified importance.
        /// </summary>
        /// <param name="importance">The importance level of the message.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="messageArgs">Optional arguments for formatting the message.</param>
        void LogMessage(MessageImportance importance = MessageImportance.Normal, string message = null, params object[] messageArgs);
    }
}
