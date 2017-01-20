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
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;

    public interface IHttpTimeoutOption<out T> { T WithTimeout(TimeSpan duration); }
    public interface IHttpUserAgentOption<out T> { T WithUserAgent(string ua); }
    public interface IHttpCookies<out T> { T WithCookies(IReadOnlyCollection<Cookie> cookies); }

    partial class HttpConfig
    {
        public static readonly HttpConfig Default;

        static HttpConfig()
        {
            var req = WebRequest.CreateHttp("http://localhost/");
            Default = new HttpConfig(TimeSpan.FromMilliseconds(req.Timeout), req.UseDefaultCredentials, req.Credentials, req.UserAgent, null);
        }
    }

    public sealed partial class HttpConfig :
        IHttpTimeoutOption<HttpConfig>,
        IHttpUserAgentOption<HttpConfig>,
        IHttpCookies<HttpConfig>
    {
        public static readonly IEnumerable<Cookie> ZeroCookies = new Cookie[0];

        public TimeSpan Timeout                    { get; private set; }
        public bool UseDefaultCredentials          { get; private set; }
        public ICredentials Credentials            { get; private set; }
        public string UserAgent                    { get; private set; }
        public IReadOnlyCollection<Cookie> Cookies { get; private set; }

        public HttpConfig(TimeSpan timeout, bool useDefaultCredentials, ICredentials credentials, string userAgent, IReadOnlyCollection<Cookie> cookies)
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

        public HttpConfig WithCookies(IReadOnlyCollection<Cookie> value) =>
            ReferenceEquals(Cookies, value) || Cookies?.SequenceEqual(value ?? ZeroCookies) == true
            ? this
            : new HttpConfig(this) { Cookies = value };
    }
}