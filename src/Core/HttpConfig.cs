#region Copyright (c) 2016 Atif Aziz. All rights reserved.

//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

#endregion

namespace WebLinq
{
    using System;
    using System.Net;

    public sealed class HttpConfig
    {
        public static readonly HttpConfig Default;

        public static IEnumerable<QueryContext, HttpConfig> Set(bool? useDefaultCredentials = null,
                                            bool? useCookies = null,
                                            string userAgent = null,
                                            TimeSpan? timeout = null) =>
            from current in Query.FindService<HttpConfig>()
            from old in Query.SetService(Set(current, useDefaultCredentials, useCookies, userAgent, timeout))
            select old;

        static HttpConfig Set(HttpConfig initial, bool? useDefaultCredentials, bool? useCookies, string userAgent, TimeSpan? timeout)
        {
            var config = initial ?? Default;

            if (useDefaultCredentials != null)
                config = config.WithUseDefaultCredentials(useDefaultCredentials.Value);
            if (useCookies != null)
                config = config.WithCookies(useCookies.Value ? new CookieContainer() : null);
            if (userAgent != null)
                config = config.WithUserAgent(userAgent);
            if (timeout != null)
                config = config.WithTimeout(timeout.Value);

            return config;
        }

        static HttpConfig()
        {
            var req = WebRequest.CreateHttp("http://localhost/");
            Default = new HttpConfig(TimeSpan.FromMilliseconds(req.Timeout), req.UseDefaultCredentials, req.Credentials, req.UserAgent, req.CookieContainer);
        }

        public TimeSpan Timeout           { get; private set; }
        public bool UseDefaultCredentials { get; private set; }
        public ICredentials Credentials   { get; private set; }
        public string UserAgent           { get; private set; }
        public CookieContainer Cookies    { get; private set; }

        public HttpConfig(TimeSpan timeout, bool useDefaultCredentials, ICredentials credentials, string userAgent, CookieContainer cookies)
        {
            Timeout     = timeout;
            UserAgent   = userAgent;
            Cookies     = cookies;
            Credentials = credentials;
            UseDefaultCredentials = useDefaultCredentials;
        }

        HttpConfig(HttpConfig other) :
            this(other.Timeout, other.UseDefaultCredentials, other.Credentials, other.UserAgent, other.Cookies) { }

        public HttpConfig WithTimeout(TimeSpan value) =>
            Timeout == value ? this : new HttpConfig(this) { Timeout = value };

        public HttpConfig WithUseDefaultCredentials(bool value) =>
            UseDefaultCredentials == value ? this : new HttpConfig(this) { UseDefaultCredentials = value };

        public HttpConfig WithCredentials(ICredentials value) =>
            Credentials == value ? this : new HttpConfig(this) { Credentials = value };

        public HttpConfig WithUserAgent(string value) =>
            WithUserAgentImpl(value ?? string.Empty);

        HttpConfig WithUserAgentImpl(string value) =>
            UserAgent == value ? this : new HttpConfig(this) { UserAgent = value };

        public HttpConfig WithCookies(CookieContainer value) =>
            Cookies == value ? this : new HttpConfig(this) { Cookies = value };
    }
}