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
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using ArgusTV.DataContracts;

namespace ArgusTV.ServiceProxy
{
    /// <exclude />
    public abstract class RestProxyBase
    {
        private string _module;

        /// <exclude />
        private HttpClient _client;

        /// <exclude />
        public RestProxyBase(string module)
        {
            _module = module;
            _client = CreateHttpClient();
        }

        /// <exclude />
        protected HttpClient CreateHttpClient()
        {
            var url = (Proxies.ServerSettings.Transport == ServiceTransport.Https ? "https://" : "http://")
                + Proxies.ServerSettings.ServerName + ":" + Proxies.ServerSettings.Port
                + "/ArgusTV/" + _module + "/";
            HttpClientHandler handler = new HttpClientHandler
            {
                Proxy = WebRequest.DefaultWebProxy,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            handler.Proxy.Credentials = CredentialCache.DefaultCredentials;
            HttpClient client = new HttpClient(handler, true)
            {
                BaseAddress = new Uri(url)
            };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            if (Proxies.ServerSettings.Transport == ServiceTransport.Https)
            {
                byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(Proxies.ServerSettings.UserName + ":" + Proxies.ServerSettings.Password);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            }
            return client;
        }

        /// <exclude />
        internal class SchedulerJsonSerializerStrategy : PocoJsonSerializerStrategy
        {
            private static readonly long _initialJavaScriptDateTicks = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
            private static readonly DateTime _minimumJavaScriptDate = new DateTime(100, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            /// <exclude />
            public override object DeserializeObject(object value, Type type)
            {
                bool isString = value is string;

                Type valueType = null;
                bool isEnum = type.IsEnum || (IsNullable(type, out valueType) && valueType.IsEnum);
                if (isEnum && (isString || value is Int32 || value is Int64))
                {
                    if (!isString
                        || Enum.IsDefined(valueType ?? type, value))
                    {
                        return Enum.Parse(valueType ?? type, value.ToString());
                    }
                }
                else if (type == typeof(DateTime)
                    || type == typeof(DateTime?))
                {
                    var s = value as string;
                    if (s != null
                        && s.StartsWith("/Date(", StringComparison.Ordinal) && s.EndsWith(")/", StringComparison.Ordinal))
                    {
                        int tzCharIndex = s.IndexOfAny(new char[] { '+', '-' }, 7);
                        long javaScriptTicks = Convert.ToInt64(s.Substring(6, (tzCharIndex > 0) ? tzCharIndex - 6 : s.Length - 8));
                        DateTime time = new DateTime((javaScriptTicks * 10000) + _initialJavaScriptDateTicks, DateTimeKind.Utc);
                        if (tzCharIndex > 0)
                        {
                            time = time.ToLocalTime();
                        }
                        return time;
                    }
                }
                else if (type == typeof(ServiceEvent))
                {
                    var jsonObject = (JsonObject)value;
                    var @event = new ServiceEvent()
                    {
                        Name = (string)jsonObject["Name"],
                        Time = (DateTime)DeserializeObject(jsonObject["Time"], typeof(DateTime))
                    };
                    var args = (JsonArray)jsonObject["Arguments"];
                    if (args != null)
                    {
                        List<object> arguments = new List<object>();
                        switch (@event.Name)
                        {
                            case DataContracts.ServiceEventNames.ConfigurationChanged:
                                arguments.Add(DeserializeObject(args[0], typeof(string)));
                                arguments.Add(DeserializeObject(args[1], typeof(string)));
                                break;

                            case DataContracts.ServiceEventNames.ScheduleChanged:
                                arguments.Add(DeserializeObject(args[0], typeof(Guid)));
                                arguments.Add(DeserializeObject(args[1], typeof(int)));
                                break;

                            case DataContracts.ServiceEventNames.RecordingStarted:
                            case DataContracts.ServiceEventNames.RecordingEnded:
                                arguments.Add(DeserializeObject(args[0], typeof(Recording)));
                                break;

                            case DataContracts.ServiceEventNames.LiveStreamStarted:
                            case DataContracts.ServiceEventNames.LiveStreamTuned:
                            case DataContracts.ServiceEventNames.LiveStreamEnded:
                            case DataContracts.ServiceEventNames.LiveStreamAborted:
                                arguments.Add(DeserializeObject(args[0], typeof(LiveStream)));
                                if (@event.Name == DataContracts.ServiceEventNames.LiveStreamAborted)
                                {
                                    arguments.Add(DeserializeObject(args[1], typeof(LiveStreamAbortReason)));
                                    arguments.Add(DeserializeObject(args[2], typeof(UpcomingProgram)));
                                }
                                break;

                            default:
                                foreach (JsonObject arg in args)
                                {
                                    arguments.Add(arg);
                                }
                                break;
                        }
                        @event.Arguments = arguments.ToArray();
                    }
                    return @event;
                }
                return base.DeserializeObject(value, type);
            }

            /// <exclude />
            protected override bool TrySerializeKnownTypes(object input, out object output)
            {
                if (input is DateTime)
                {
                    DateTime value = (DateTime)input;
                    DateTime time = value.ToUniversalTime();

                    string suffix = "";
                    if (value.Kind != DateTimeKind.Utc)
                    {
                        TimeSpan localTZOffset;
                        if (value >= time)
                        {
                            localTZOffset = value - time;
                            suffix = "+";
                        }
                        else
                        {
                            localTZOffset = time - value;
                            suffix = "-";
                        }
                        suffix += localTZOffset.ToString("hhmm");
                    }

                    if (time < _minimumJavaScriptDate)
                    {
                        time = _minimumJavaScriptDate;
                    }
                    long ticks = (time.Ticks - _initialJavaScriptDateTicks) / (long)10000;
                    output = "/Date(" + ticks + suffix + ")/";
                    return true;
                }
                return base.TrySerializeKnownTypes(input, out output);
            }

            private static bool IsNullable(Type type, out Type valueType)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    valueType = type.GetGenericArguments()[0];
                    return true;
                }

                valueType = null;
                return false;
            }
        }

        /// <exclude />
        protected HttpRequestMessage NewRequest(HttpMethod method, string url, params object[] args)
        {
            if (url.StartsWith("/"))
            {
                url = url.Substring(1);
            }
            if (args != null && args.Length > 0)
            {
                List<object> encodedArgs = new List<object>();
                foreach (var arg in args)
                {
                    string urlArg;
                    if (arg is DateTime)
                    {
                        DateTime time = (DateTime)arg;
                        if (time.Kind == DateTimeKind.Unspecified)
                        {
                            urlArg = new DateTime(time.Ticks, DateTimeKind.Local).ToString("o");
                        }
                        urlArg = time.ToString("o");
                    }
                    else
                    {
                        urlArg = arg.ToString();
                    }
                    encodedArgs.Add(HttpUtility.UrlEncode(urlArg));
                }
                return new HttpRequestMessage(method, String.Format(url, encodedArgs.ToArray()));
            }
            return new HttpRequestMessage(method, url);
        }

        /// <exclude />
        protected bool IsConnectionError(Exception ex)
        {
            var requestException = ex as HttpRequestException;
            if (requestException != null)
            {
                var webException = requestException.InnerException as WebException;
                if (webException != null)
                {
                    switch (webException.Status)
                    {
                        case System.Net.WebExceptionStatus.ConnectFailure:
                        case System.Net.WebExceptionStatus.NameResolutionFailure:
                        case System.Net.WebExceptionStatus.ProxyNameResolutionFailure:
                        case System.Net.WebExceptionStatus.RequestProhibitedByProxy:
                        case System.Net.WebExceptionStatus.SecureChannelFailure:
                        case System.Net.WebExceptionStatus.TrustFailure:
                            return true;
                    }
                }
            }
            return false;
        }

        /// <exclude />
        protected async Task ExecuteAsync(HttpRequestMessage request, bool logError = true)
        {
            try
            {
                using (var response = await ExecuteRequestAsync(request, logError).ConfigureAwait(false))
                {
                }
            }
            finally
            {
                request.Dispose();
            }
        }

        /// <exclude />
        protected async Task<T> ExecuteAsync<T>(HttpRequestMessage request, bool logError = true)
            where T : new()
        {
            try
            {
                using (var response = await ExecuteRequestAsync(request, logError).ConfigureAwait(false))
                {
                    return await DeserializeResponseContentAsync<T>(response).ConfigureAwait(false);
                }
            }
            catch (ArgusTVException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (logError)
                {
                    Proxies.Logger.Error(ex.ToString());
                }
                throw new ArgusTVUnexpectedErrorException("An unexpected error occured.");
            }
            finally
            {
                request.Dispose();
            }
        }

        /// <exclude/>
        protected async Task<HttpResponseMessage> ExecuteRequestAsync(HttpRequestMessage request, bool logError = true)
        {
            try
            {
                if (request.Method != HttpMethod.Get && request.Content == null)
                {
                    // Work around current Mono bug.
                    request.Content = new ByteArrayContent(new byte[0]);
                }
                var response = await _client.SendAsync(request).ConfigureAwait(false);
                if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                {
                    var error = SimpleJson.DeserializeObject<RestError>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                    throw new ArgusTVException(error.detail);
                }
                if (response.StatusCode >= HttpStatusCode.BadRequest)
                {
                    throw new ArgusTVException(response.ReasonPhrase);
                }
                return response;
            }
            catch (AggregateException ex)
            {
                if (IsConnectionError(ex.InnerException))
                {
                    WakeOnLan.EnsureServerAwake(Proxies.ServerSettings);

                    throw new ArgusTVNotFoundException(ex.InnerException.InnerException.Message);
                }
                throw;
            }
            catch (ArgusTVException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (logError)
                {
                    Proxies.Logger.Error(ex.ToString());
                }
                throw new ArgusTVUnexpectedErrorException("An unexpected error occured.");
            }
        }

        /// <exclude />
        protected async static Task<T> DeserializeResponseContentAsync<T>(HttpResponseMessage response)
            where T : new()
        {
            string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (String.IsNullOrEmpty(content))
            {
                return default(T);
            }
            return SimpleJson.DeserializeObject<T>(content, new SchedulerJsonSerializerStrategy());
        }

        private class RestError
        {
            public string detail { get; set; }
        }
    }

    /// <exclude />
    public static class HttpRequestMessageExtensions
    {
        /// <exclude />
        public static void AddBody(this HttpRequestMessage request, object body)
        {
            request.Content = new StringContent(
                SimpleJson.SerializeObject(body, new RestProxyBase.SchedulerJsonSerializerStrategy()), Encoding.UTF8, "application/json");
        }

        /// <exclude />
        public static void AddParameter(this HttpRequestMessage request, string name, object value)
        {
            string url = request.RequestUri.OriginalString;
            url += (url.Contains("?") ? "&" : "?") + name + "=" + HttpUtility.UrlEncode(value.ToString());
            request.RequestUri = new Uri(url, UriKind.Relative);
        }
    }
}
