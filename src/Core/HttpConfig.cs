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

    partial class HttpConfig
    {
        public static readonly HttpConfig Default = InitDefault();

        static HttpConfig InitDefault()
        {
#pragma warning disable SYSLIB0014 // WebRequest, HttpWebRequest, ServicePoint, WebClient are obsolete
            var req = WebRequest.CreateHttp(new Uri("http://localhost/"));
#pragma warning restore SYSLIB0014 // WebRequest, HttpWebRequest, ServicePoint, WebClient are obsolete

            return new HttpConfig(HttpHeaderCollection.Empty,
                                  TimeSpan.FromMilliseconds(req.Timeout),
                                  req.UseDefaultCredentials, req.Credentials,
                                  req.UserAgent ?? string.Empty,
                                  req.AutomaticDecompression,
                                  ignoreInvalidServerCertificate: false);
        }
    }

    public sealed partial class HttpConfig
    {
        public HttpHeaderCollection Headers { get; init; }
        public TimeSpan Timeout { get; init; }
        public bool UseDefaultCredentials { get; init; }
        public ICredentials? Credentials { get; init; }
        public string UserAgent { get; init; }
        public DecompressionMethods AutomaticDecompression { get; init; }
        public bool IgnoreInvalidServerCertificate { get; init; }

        public HttpConfig(HttpHeaderCollection headers,
                          TimeSpan timeout,
                          bool useDefaultCredentials, ICredentials? credentials,
                          string userAgent,
                          DecompressionMethods automaticDecompression,
                          bool ignoreInvalidServerCertificate)
        {
            Headers = headers;
            Timeout = timeout;
            UserAgent = userAgent;
            AutomaticDecompression = automaticDecompression;
            Credentials = credentials;
            UseDefaultCredentials = useDefaultCredentials;
            IgnoreInvalidServerCertificate = ignoreInvalidServerCertificate;
        }

        HttpConfig(HttpConfig other) :
            this(other.Headers,
                 other.Timeout,
                 other.UseDefaultCredentials, other.Credentials,
                 other.UserAgent,
                 other.AutomaticDecompression,
                 other.IgnoreInvalidServerCertificate) { }

        public HttpConfig WithHeader(string name, string value) =>
            WithHeaders(Headers.Set(name, value));

        public HttpConfig WithHeaders(HttpHeaderCollection value) =>
            Headers == value ? this : new HttpConfig(this) { Headers = value };

        public HttpConfig WithTimeout(TimeSpan value) =>
            Timeout == value ? this : new HttpConfig(this) { Timeout = value };

        public HttpConfig WithUseDefaultCredentials(bool value) =>
            UseDefaultCredentials == value ? this : new HttpConfig(this) { UseDefaultCredentials = value };

        public HttpConfig WithCredentials(ICredentials value) =>
            Credentials == value ? this : new HttpConfig(this) { Credentials = value };

        public HttpConfig WithUserAgent(string value) =>
            UserAgent == value ? this : new HttpConfig(this) { UserAgent = value };

        public HttpConfig WithAutomaticDecompression(DecompressionMethods value) =>
            AutomaticDecompression == value ? this : new HttpConfig(this) { AutomaticDecompression = value };

        public HttpConfig WithIgnoreInvalidServerCertificate(bool value) =>
            IgnoreInvalidServerCertificate == value
            ? this
            : new HttpConfig(this) { IgnoreInvalidServerCertificate = value };
    }
}
