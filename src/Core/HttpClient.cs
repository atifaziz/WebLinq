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
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Mannex.Collections.Generic;
    using Mannex.Collections.Specialized;

    public sealed class HttpOptions
    {
        public bool ReturnErrorneousFetch { get; set; }
    }

    public interface IHttpClient<T>
    {
        T Config { get; }
        IHttpClient<T> WithConfig(T config);
        Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, T config, HttpOptions options);
    }

    public static class HttpClientExtensions
    {
        public static IHttpClient<T> Mutable<T>(this IHttpClient<T> client) =>
            new MutableHttpClient<T>(client);

        sealed class MutableHttpClient<T> : IHttpClient<T>
        {
            readonly IHttpClient<T> _client;

            public MutableHttpClient(IHttpClient<T> client)
            {
                _client = client;
            }

            public T Config { get; private set; }
            public IHttpClient<T> WithConfig(T config)
            {
                Config = config;
                return this;
            }

            public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, T config, HttpOptions options) =>
                _client.WithConfig(Config).SendAsync(request, config, options);
        }
    }

    public static class HttpClient
    {
        public static IHttpClient<HttpConfig> Default = new DefaultHttpClient(HttpConfig.Default);
    }

    sealed class DefaultHttpClient : IHttpClient<HttpConfig>
    {
        public HttpConfig Config { get; }

        public DefaultHttpClient(HttpConfig config)
        {
            Config = config;
        }

        public IHttpClient<HttpConfig> WithConfig(HttpConfig config) =>
            new DefaultHttpClient(config);

        public void Register(Action<Type, object> registrationHandler) =>
            registrationHandler(typeof(IHttpClient<HttpConfig>), this);

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpConfig config, HttpOptions options) =>
            Send(request, config ?? Config, options);

        static async Task<HttpResponseMessage> Send(HttpRequestMessage request, HttpConfig config, HttpOptions options)
        {
            var hwreq = WebRequest.CreateHttp(request.RequestUri);

            hwreq.Method                = request.Method.Method;
            hwreq.Timeout               = (int)config.Timeout.TotalMilliseconds;
            hwreq.Credentials           = config.Credentials;
            hwreq.UseDefaultCredentials = config.UseDefaultCredentials;
            hwreq.AllowAutoRedirect     = false;

            if (config.IgnoreInvalidServerCertificate)
                hwreq.ServerCertificateValidationCallback = delegate { return true; };

            if (config.Cookies?.Any() == true)
            {
                CookieContainer cookies;
                hwreq.CookieContainer = cookies = new CookieContainer();
                foreach (var cookie in config.Cookies)
                    cookies.Add(cookie);
            }

            var userAgent = request.Headers.UserAgent.ToString();
            hwreq.UserAgent = userAgent.Length > 0 ? userAgent : config.UserAgent;

            var referrer = request.Headers.Referrer;
            if (referrer != null)
                hwreq.Referer = referrer.ToString();

            var accept = request.Headers.Accept.ToString();
            if (accept.Length > 0)
                hwreq.Accept = accept;

            var content = request.Content;
            foreach (var e in from e in request.Headers.Concat(content?.Headers ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>())
                              where !e.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)
                                 && !e.Key.Equals("User-Agent", StringComparison.OrdinalIgnoreCase)
                                 && !e.Key.Equals("Referer", StringComparison.OrdinalIgnoreCase)
                                 && !e.Key.Equals("Accept", StringComparison.OrdinalIgnoreCase)
                              from v in e.Value
                              select e.Key.AsKeyTo(v))
            {
                hwreq.Headers.Add(e.Key, e.Value);
            }

            try
            {
                if (content != null)
                {
                    // ReSharper disable once PossibleNullReferenceException
                    hwreq.ContentType = content.Headers.ContentType.ToString();
                    using (var s = hwreq.GetRequestStream())
                        await content.CopyToAsync(s).DontContinueOnCapturedContext();
                }
                return CreateResponse(hwreq, (HttpWebResponse) await hwreq.GetResponseAsync().DontContinueOnCapturedContext());
            }
            catch (WebException e) when (e.Status == WebExceptionStatus.ProtocolError)
            {
                if (options?.ReturnErrorneousFetch == false)
                    throw;
                return CreateResponse(hwreq, (HttpWebResponse) e.Response);
            }
        }

        static HttpResponseMessage CreateResponse(HttpWebRequest req, HttpWebResponse rsp)
        {
            try
            {
                var response = new HttpResponseMessage(rsp.StatusCode)
                {
                    Version        = rsp.ProtocolVersion,
                    ReasonPhrase   = rsp.StatusDescription,
                    Content        = new StreamContent(rsp.GetResponseStream()),
                    RequestMessage = new HttpRequestMessage(ParseHttpMethod(req.Method), rsp.ResponseUri),
                };

                var headers =
                    from e in rsp.Headers.AsEnumerable()
                    group e by e.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase)
                            || e.Key.Equals("Expires", StringComparison.OrdinalIgnoreCase)
                            || e.Key.Equals("Last-Modified", StringComparison.OrdinalIgnoreCase)
                            || e.Key.Equals("Allow", StringComparison.OrdinalIgnoreCase) into g
                    from e in g
                    select new
                    {
                        Headers = g.Key
                                ? (HttpHeaders) response.Content.Headers
                                : response.Headers,
                        e.Key,
                        e.Value,
                    };

                foreach (var e in headers)
                {
                    if (!e.Headers.TryAddWithoutValidation(e.Key, e.Value))
                        throw new Exception($"Invalid HTTP header: {e.Key}: {e.Value}");
                }

                rsp = null; // ownership passed on to StreamContent
                return response;
            }
            finally
            {
                rsp?.Dispose();
            }
        }

        static HttpMethod ParseHttpMethod(string method) =>
            HttpMethods.GetValue(method, m => new FormatException($"'{m}' is not a valid HTTP method."));

        static readonly Dictionary<string, HttpMethod> HttpMethods = new[]
            {
                HttpMethod.Get    ,
                HttpMethod.Post   ,
                HttpMethod.Put    ,
                HttpMethod.Delete ,
                HttpMethod.Options,
                HttpMethod.Head   ,
                HttpMethod.Trace  ,
            }
            .ToDictionary(e => e.Method, e => e, StringComparer.OrdinalIgnoreCase);
    }
}