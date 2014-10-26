using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArgusTV.ServiceProxy
{
    /// <summary>
    /// The service proxy logger interface.
    /// </summary>
    public interface IServiceProxyLogger
    {
        /// <summary>
        /// Log a verbose/debug message.
        /// </summary>
        /// <param name="message">The message to log (format string).</param>
        /// <param name="args">Optional message parameters.</param>
        void Verbose(string message, params object[] args);

        /// <summary>
        /// Log an informational message.
        /// </summary>
        /// <param name="message">The message to log (format string).</param>
        /// <param name="args">Optional message parameters.</param>
        void Info(string message, params object[] args);

        /// <summary>
        /// Log a warning message.
        /// </summary>
        /// <param name="message">The message to log (format string).</param>
        /// <param name="args">Optional message parameters.</param>
        void Warn(string message, params object[] args);

        /// <summary>
        /// Log an error message.
        /// </summary>
        /// <param name="message">The message to log (format string).</param>
        /// <param name="args">Optional message parameters.</param>
        void Error(string message, params object[] args);
    }
}
