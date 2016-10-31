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

    public sealed class HttpQueryState
    {
        public static HttpQueryState Default;

        static HttpQueryState()
        {
            var req = WebRequest.CreateHttp("http://localhost/");
            Default = new HttpQueryState(TimeSpan.FromMilliseconds(req.Timeout), req.UseDefaultCredentials, req.Credentials, req.UserAgent, req.CookieContainer);
        }

        public TimeSpan Timeout           { get; private set; }
        public bool UseDefaultCredentials { get; private set; }
        public ICredentials Credentials   { get; private set; }
        public string UserAgent           { get; private set; }
        public CookieContainer Cookies    { get; private set; }

        public HttpQueryState(TimeSpan timeout, bool useDefaultCredentials, ICredentials credentials, string userAgent, CookieContainer cookies)
        {
            Timeout     = timeout;
            UserAgent   = userAgent;
            Cookies     = cookies;
            Credentials = credentials;
            UseDefaultCredentials = useDefaultCredentials;
        }

        HttpQueryState(HttpQueryState other) :
            this(other.Timeout, other.UseDefaultCredentials, other.Credentials, other.UserAgent, other.Cookies) { }

        public HttpQueryState WithTimeout(TimeSpan value) =>
            Timeout == value ? this : new HttpQueryState(this) { Timeout = value };

        public HttpQueryState WithUseDefaultCredentials(bool value) =>
            UseDefaultCredentials == value ? this : new HttpQueryState(this) { UseDefaultCredentials = value };

        public HttpQueryState WithCredentials(ICredentials value) =>
            Credentials == value ? this : new HttpQueryState(this) { Credentials = value };

        public HttpQueryState WithUserAgent(string value) =>
            WithUserAgentImpl(value ?? string.Empty);

        HttpQueryState WithUserAgentImpl(string value) =>
            UserAgent == value ? this : new HttpQueryState(this) { UserAgent = value };

        public HttpQueryState WithCookies(CookieContainer value) =>
            Cookies == value ? this : new HttpQueryState(this) { Cookies = value };
    }
}