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

    public interface IHttpClient
    {
        HttpConfig Config { get; }
        IHttpClient WithConfig(HttpConfig config);
        Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpConfig config);
    }

    public static class HttpClientExtensions
    {
        public static IHttpClient SetHeader(this IHttpClient client, string name, string value) =>
            client.WithConfig(client.Config.WithHeader(name, value));

        public static IHttpClient Mutable<T>(this IHttpClient client) =>
            new MutableHttpClient(client);

        sealed class MutableHttpClient : IHttpClient
        {
            readonly IHttpClient _client;

            public MutableHttpClient(IHttpClient client)
            {
                _client = client;
            }

            public HttpConfig Config { get; private set; }
            public IHttpClient WithConfig(HttpConfig config)
            {
                Config = config;
                return this;
            }

            public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpConfig config) =>
                _client.WithConfig(Config).SendAsync(request, config);
        }
    }

    public static class HttpClient
    {
        public static IHttpClient Default = new DefaultHttpClient(HttpConfig.Default);

        public static IHttpClient Wrap(this IHttpClient client,
            Func<Func<HttpRequestMessage, HttpConfig, Task<HttpResponseMessage>>, HttpRequestMessage, HttpConfig, Task<HttpResponseMessage>> send) =>
            new DelegatingHttpClient(client, send);

        sealed class DelegatingHttpClient : IHttpClient
        {
            readonly IHttpClient _client;
            readonly Func<Func<HttpRequestMessage, HttpConfig, Task<HttpResponseMessage>>, HttpRequestMessage, HttpConfig, Task<HttpResponseMessage>> _send;

            public DelegatingHttpClient(IHttpClient client,
                Func<Func<HttpRequestMessage, HttpConfig, Task<HttpResponseMessage>>, HttpRequestMessage, HttpConfig, Task<HttpResponseMessage>> send)
            {
                _client = client;
                _send = send ?? ((super, request, config) => super(request, config));
            }

            public HttpConfig Config => _client.Config;

            public IHttpClient WithConfig(HttpConfig config) =>
                new DelegatingHttpClient(_client.WithConfig(config), _send);

            public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpConfig config) =>
                _send(_client.SendAsync, request, config);
        }
    }

    sealed class DefaultHttpClient : IHttpClient
    {
        public HttpConfig Config { get; }

        public DefaultHttpClient(HttpConfig config)
        {
            Config = config;
        }

        public IHttpClient WithConfig(HttpConfig config) =>
            new DefaultHttpClient(config);

        public void Register(Action<Type, object> registrationHandler) =>
            registrationHandler(typeof(IHttpClient), this);

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpConfig config) =>
            Send(request, config ?? Config);

        static async Task<HttpResponseMessage> Send(HttpRequestMessage request, HttpConfig config)
        {
            var requestUrl = request.RequestUri;

            // Following is workaround for a compatibility bug in .NET Core:
            // https://github.com/dotnet/corefx/issues/39618

            if (requestUrl.Fragment.Length > 0)
                requestUrl = new Uri(requestUrl.GetComponents(UriComponents.AbsoluteUri & ~UriComponents.Fragment, UriFormat.SafeUnescaped));

            var hwreq = WebRequest.CreateHttp(requestUrl);

            hwreq.Method                = request.Method.Method;
            hwreq.Timeout               = (int)config.Timeout.TotalMilliseconds;
            hwreq.AllowAutoRedirect     = false;

            if (config.Credentials != null)
                hwreq.Credentials = config.Credentials;
            else
                hwreq.UseDefaultCredentials = config.UseDefaultCredentials;

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

            hwreq.AutomaticDecompression = config.AutomaticDecompression;

            if (request.Headers.Referrer is Uri referrerUrl)
                hwreq.Referer = referrerUrl.AbsoluteUri;
            else if (config.Headers.TryGetValue("Referer", out var referrer))
                hwreq.Referer = referrer.FirstOrDefault();

            var accept = request.Headers.Accept.ToString();
            if (accept.Length > 0)
                hwreq.Accept = accept;
            else if (config.Headers.TryGetValue("Accept", out var configAccept))
                hwreq.Accept = configAccept;

            var content = request.Content;
            foreach (var e in from e in request.Headers.Concat(content?.Headers ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>())
                                                       .Concat(from e in config.Headers select e.Key.AsKeyTo(e.Value.AsEnumerable()))
                                                       .ToLookup(e => e.Key, e => e.Value)
                                                       .Select(g => g.Key.AsKeyTo(g.First()))
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

                // IMPORTANT! DO NOT access header values over the the
                // `HttpWebResponse.Headers.GetValues(int)` overload since it has a regression with
                // .NET Framework and can fold headers incorrectly, with the most notable case
                // being "Set-Cookie". See https://github.com/dotnet/corefx/issues/39527 for more.

                var sourceHeaders = rsp.Headers;

                var headers =
                    from k in sourceHeaders.AllKeys
                    select k.AsKeyTo(sourceHeaders.GetValues(k)) into e
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
