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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using ArgusTV.DataContracts;

namespace ArgusTV.ServiceProxy
{
    /// <summary>
    /// Core service proxy.
    /// </summary>
    public partial class CoreServiceProxy : RestProxyBase
    {
        /// <summary>
        /// Constructs a channel to the service.
        /// </summary>
        internal CoreServiceProxy()
            : base("Core")
        {
            _listenerClient = CreateHttpClient();
            _listenerClient.Timeout = TimeSpan.FromDays(1);
        }

        #region Initialization

        /// <summary>
        /// Ping ARGUS TV server and test the API version.
        /// </summary>
        /// <param name="requestedApiVersion">The API version the client needs, pass in Constants.CurrentApiVersion.</param>
        /// <returns>0 if client and server are compatible, -1 if the client is too old and +1 if the client is newer than the server.</returns>
        public async Task<int> Ping(int requestedApiVersion)
        {
            var request = NewRequest(HttpMethod.Get, "Ping/{0}", requestedApiVersion);
            var result = await ExecuteAsync<PingResult>(request, logError: false).ConfigureAwait(false);
            return result.Result;
        }

        private class PingResult
        {
            public int Result { get; set; }
        }

        /// <summary>
        /// Get the server's MAC address(es).  These can be stored on the client after a successful
        /// connect and later used to re-connect with wake-on-lan.
        /// </summary>
        /// <returns>An array containing one or more MAC addresses in HEX string format (e.g. "A1B2C3D4E5F6").</returns>
        public async Task<IEnumerable<string>> GetMacAddresses()
        {
            var request = NewRequest(HttpMethod.Get, "GetMacAddresses");
            return await ExecuteAsync<List<string>>(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Check to see if there is a newer version of ARGUS TV available online.
        /// </summary>
        /// <returns>A NewVersionInfo object if there is a newer version available, null if the current installation is up-to-date.</returns>
        public async Task<NewVersionInfo> IsNewerVersionAvailable()
        {
            var request = NewRequest(HttpMethod.Get, "IsNewerVersionAvailable");
            return await ExecuteAsync<NewVersionInfo>(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Get the server's version as a string.
        /// </summary>
        /// <returns>Returns the ARGUS TV product version, for display purposes. Don't use this to do version checking!</returns>
        public async Task<string> GetServerVersion()
        {
            var request = NewRequest(HttpMethod.Get, "Version");
            var result = await ExecuteAsync<GetVersionResult>(request).ConfigureAwait(false);
            return result.Version;
        }

        private class GetVersionResult
        {
            public string Version { get; set; }
        }

        #endregion

        #region Event Listeners

        private HttpClient _listenerClient;

        /// <summary>
        /// Subscribe your client to a group of ARGUS TV events using a polling mechanism.
        /// </summary>
        /// <param name="uniqueClientId">The unique ID (e.g. your DNS hostname combined with a constant GUID) to identify your client.</param>
        /// <param name="eventGroups">The event group(s) to subscribe to (flags can be OR'd).</param>
        public async Task SubscribeServiceEvents(string uniqueClientId, EventGroup eventGroups)
        {
            var request = NewRequest(HttpMethod.Post, "ServiceEvents/{0}/Subscribe/{1}", uniqueClientId, eventGroups);
            await ExecuteAsync(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Unsubscribe your client from all ARGUS TV events.
        /// </summary>
        /// <param name="uniqueClientId">The unique ID (e.g. your DNS hostname combined with a constant GUID) to identify your client.</param>
        public async Task UnsubscribeServiceEvents(string uniqueClientId)
        {
            var request = NewRequest(HttpMethod.Post, "ServiceEvents/{0}/Unsubscribe", uniqueClientId);
            await ExecuteAsync(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Get all queued ARGUS TV events for your client. Call this every X seconds to get informed at a regular interval about what happened.
        /// </summary>
        /// <param name="uniqueClientId">The unique ID (e.g. your DNS hostname combined with a constant GUID) to identify your client.</param>
        /// <param name="cancellationToken">The cancellation token to potentially abort the request (or CancellationToken.None).</param>
        /// <param name="timeoutSeconds">The maximum timeout of the request (default is 5 minutes).</param>
        /// <returns>Zero or more service events, or null in case your subscription has expired.</returns>
        public async Task<List<ServiceEvent>> GetServiceEvents(string uniqueClientId, CancellationToken cancellationToken, int timeoutSeconds = 300)
        {
            var request = NewRequest(HttpMethod.Get, "ServiceEvents/{0}/{1}", uniqueClientId, timeoutSeconds);

            // By default return an empty list (e.g. in case of a timeout or abort), the client will simply need to call this again.
            GetServiceEventsResult result = new GetServiceEventsResult()
            {
                Events = new List<ServiceEvent>()
            };

#if DOTNET4
            using (var timeoutCancellationSource = new CancellationTokenSource())
#else
            using (var timeoutCancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(timeoutSeconds, 30))))
#endif
            using (var linkedCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCancellationSource.Token))
            {
#if DOTNET4
                Task.Factory.StartNew(() =>
                {
                    cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(Math.Max(timeoutSeconds, 30)));
                    timeoutCancellationSource.Cancel();
                }).ConfigureAwait(false);
#endif

                try
                {
                    using (var response = await _listenerClient.SendAsync(request, linkedCancellationSource.Token))
                    {
                        result = await DeserializeResponseContentAsync<GetServiceEventsResult>(response);
                    }
                }
                catch (AggregateException ex)
                {
                    if (IsConnectionError(ex.InnerException))
                    {
                        throw new ArgusTVNotFoundException(ex.InnerException.InnerException.Message);
                    }
                    // Check if we're dealing with either a timeout or an explicit cancellation (same exception in both cases).
                    if (!(ex.InnerException is TaskCanceledException))
                    {
                        throw;
                    }
                }
            }

            if (result == null
                || result.Expired)
            {
                return null;
            }
            return result.Events;
        }

        private class GetServiceEventsResult
        {
            public bool Expired { get; set; }
            public List<ServiceEvent> Events { get; set; }
        }

        #endregion

        #region Power Management

        /// <summary>
        /// Tell the server we'd like to keep it alive for a little longer.  A client
        /// should call this method every two minutes or so to keep the server from
        /// entering standby (if it is configured to do so).
        /// </summary>
        public void KeepServerAlive()
        {
            var request = NewRequest(HttpMethod.Post, "KeepServerAlive");
            ExecuteAsync(request).ConfigureAwait(false); // no need to await this, we send the keep-alive fully async
        }

        #endregion
    }
}
