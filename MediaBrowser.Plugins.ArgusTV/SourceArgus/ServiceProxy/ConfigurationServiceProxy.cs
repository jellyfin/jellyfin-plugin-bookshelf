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
using System.Threading.Tasks;

using ArgusTV.DataContracts;

namespace ArgusTV.ServiceProxy
{
    /// <summary>
    /// Configuration settings service.
    /// </summary>
    public partial class ConfigurationServiceProxy : RestProxyBase
    {
        /// <summary>
        /// Constructs a channel to the service.
        /// </summary>
        internal ConfigurationServiceProxy()
            : base("Configuration")
        {
        }

        /// <summary>
        /// Get integer setting.
        /// </summary>
        /// <param name="module">The module of the setting.</param>
        /// <param name="key">The setting's unique key within the module.</param>
        /// <returns>The integer setting value, or null if no setting was found.</returns>
        public async Task<int?> GetIntValue(string module, string key)
        {
            var request = NewRequest(HttpMethod.Get, "IntegerValue/{0}/{1}", module, key);
            var result = await ExecuteAsync<GetValueResult<int?>>(request).ConfigureAwait(false);
            return result.Value;
        }

        /// <summary>
        /// Get string setting.
        /// </summary>
        /// <param name="module">The module of the setting.</param>
        /// <param name="key">The setting's unique key within the module.</param>
        /// <returns>The string setting value, or null if no setting was found.</returns>
        public async Task<string> GetStringValue(string module, string key)
        {
            var request = NewRequest(HttpMethod.Get, "Value/{0}/{1}", module, key);
            var result = await ExecuteAsync<GetValueResult<string>>(request).ConfigureAwait(false);
            return result.Value;
        }

        /// <summary>
        /// Get boolean setting.
        /// </summary>
        /// <param name="module">The module of the setting.</param>
        /// <param name="key">The setting's unique key within the module.</param>
        /// <returns>The boolean setting value, or null if no setting was found.</returns>
        public async Task<bool?> GetBooleanValue(string module, string key)
        {
            var request = NewRequest(HttpMethod.Get, "BooleanValue/{0}/{1}", module, key);
            var result = await ExecuteAsync<GetValueResult<bool?>>(request).ConfigureAwait(false);
            return result.Value;
        }

        /// <summary>
        /// Set integer setting value.
        /// </summary>
        /// <param name="module">The module of the setting.</param>
        /// <param name="key">The setting's unique key within the module.</param>
        /// <param name="value">The new value of the setting.</param>
        public async Task SetIntValue(string module, string key, int? value)
        {
            var request = NewRequest(HttpMethod.Post, "SetIntegerValue");
            request.AddBody(new
            {
                Module = module,
                Key = key,
                Value = value
            });
            await ExecuteAsync(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Set string setting value.
        /// </summary>
        /// <param name="module">The module of the setting.</param>
        /// <param name="key">The setting's unique key within the module.</param>
        /// <param name="value">The new value of the setting.</param>
        public async Task SetStringValue(string module, string key, string value)
        {
            var request = NewRequest(HttpMethod.Post, "SetValue");
            request.AddBody(new
            {
                Module = module,
                Key = key,
                Value = value
            });
            await ExecuteAsync(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Set boolean setting value.
        /// </summary>
        /// <param name="module">The module of the setting.</param>
        /// <param name="key">The setting's unique key within the module.</param>
        /// <param name="value">The new value of the setting.</param>
        public async Task SetBooleanValue(string module, string key, bool? value)
        {
            var request = NewRequest(HttpMethod.Post, "SetBooleanValue");
            request.AddBody(new
            {
                Module = module,
                Key = key,
                Value = value
            });
            await ExecuteAsync(request).ConfigureAwait(false);
        }

        private class GetValueResult<T>
        {
            public T Value { get; set; }
        }
    }
}
