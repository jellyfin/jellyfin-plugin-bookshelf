/*
 *	Copyright (C) 2007-2014 ARGUS TV
 *	http://www.argus-tv.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA.
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

using ArgusTV.DataContracts;
using System.Threading.Tasks;

namespace ArgusTV.ServiceProxy
{
    /// <summary>
    /// High-level logging service.
    /// </summary>
    public partial class LogServiceProxy : RestProxyBase
    {
        /// <summary>
        /// Constructs a channel to the service.
        /// </summary>
        internal LogServiceProxy()
            : base("Log")
        {
        }

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="module">The name of the module that is logging the message.</param>
        /// <param name="logSeverity">The severity of the message.</param>
        /// <param name="message">The message text.</param>
        public async Task LogMessage(string module, LogSeverity logSeverity, string message)
        {
            var request = NewRequest(HttpMethod.Post, "Message");
            request.AddBody(new
            {
                Module = module,
                Severity = logSeverity,
                Message = message
            });
            await ExecuteAsync(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Get all log entries for a certain module.
        /// </summary>
        /// <param name="lowerDate">Return messages logged after this date.</param>
        /// <param name="upperDate">Return messages logged before this date.</param>
        /// <param name="maxEntries">The maximum number of messages to return.</param>
        /// <param name="module">The name of the module, or null.</param>
        /// <param name="severity">The severity of the messages, or null.</param>
        /// <returns>An array containing zero or more log message and 'maxEntriesReached' (set to true if more than 'maxEntries' messages where available).</returns>
        public async Task<LogEntriesResult> GetLogEntries(DateTime lowerDate, DateTime upperDate, int maxEntries, string module, LogSeverity? severity)
        {
            var request = NewRequest(HttpMethod.Get, "Entries/{0}/{1}/{2}", lowerDate, upperDate, maxEntries);
            if (module != null)
            {
                request.AddParameter("module", module);
            }
            if (severity.HasValue)
            {
                request.AddParameter("severity", severity.Value);
            }
            return await ExecuteAsync<LogEntriesResult>(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Get all available modules currently in the log.
        /// </summary>
        /// <returns>An array containing zero or more module names.</returns>
        public async Task<List<string>> GetAllModules()
        {
            var request = NewRequest(HttpMethod.Get, "Modules");
            return await ExecuteAsync<List<string>>(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Send a test-mail using the current SMTP settings.
        /// </summary>
        public async Task SendTestMail()
        {
            var request = NewRequest(HttpMethod.Post, "TestMail");
            await ExecuteAsync(request).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Result from getting log entries.
    /// </summary>
    public class LogEntriesResult
    {
        /// <summary>
        /// An array containing zero or more log messages.
        /// </summary>
        public List<LogEntry> LogEntries { get; set; }

        /// <summary>
        /// Set to true if more than 'maxEntries' messages where available.
        /// </summary>
        public bool MaxEntriesReached { get; set; }
    }
}
