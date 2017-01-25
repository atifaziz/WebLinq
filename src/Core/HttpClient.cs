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
    using System.IO;
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

    public class HttpClient : IHttpClient<HttpConfig>
    {
        public HttpConfig Config { get; }

        public HttpClient(HttpConfig config)
        {
            Config = config;
        }

        public IHttpClient<HttpConfig> WithConfig(HttpConfig config) =>
            new HttpClient(config);

        public void Register(Action<Type, object> registrationHandler) =>
            registrationHandler(typeof(IHttpClient<HttpConfig>), this);

        public virtual Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpConfig config, HttpOptions options) =>
            Send(request, config ?? Config, options);

        static async Task<HttpResponseMessage> Send(HttpRequestMessage request, HttpConfig config, HttpOptions options)
        {
            var hwreq = WebRequest.CreateHttp(request.RequestUri);

            hwreq.Method                = request.Method.Method;
            hwreq.Timeout               = (int)config.Timeout.TotalMilliseconds;
            hwreq.Credentials           = config.Credentials;
            hwreq.UseDefaultCredentials = config.UseDefaultCredentials;
            hwreq.AllowAutoRedirect     = false;

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

            HttpWebResponse hwrsp = null;
            try
            {
                if (content != null)
                {
                    // ReSharper disable once PossibleNullReferenceException
                    hwreq.ContentType = content.Headers.ContentType.ToString();
                    using (var s = hwreq.GetRequestStream())
                        await content.CopyToAsync(s).DontContinueOnCapturedContext();
                }
                return await CreateResponse(hwreq, hwrsp = (HttpWebResponse) await hwreq.GetResponseAsync()).DontContinueOnCapturedContext();
            }
            catch (WebException e) when (e.Status == WebExceptionStatus.ProtocolError)
            {
                if (options?.ReturnErrorneousFetch == false)
                    throw;
                return await CreateResponse(hwreq, hwrsp = (HttpWebResponse)e.Response).DontContinueOnCapturedContext();
            }
            finally
            {
                hwrsp?.Dispose();
            }
        }

        static async Task<HttpResponseMessage> CreateResponse(HttpWebRequest req, HttpWebResponse rsp)
        {
            var ms = new MemoryStream();
            using (var s = rsp.GetResponseStream())
            {
                if (s != null)
                    await s.CopyToAsync(ms).DontContinueOnCapturedContext();

            }
            ms.Position = 0;
            var response = new HttpResponseMessage(rsp.StatusCode)
            {
                Version        = rsp.ProtocolVersion,
                ReasonPhrase   = rsp.StatusDescription,
                Content        = new StreamContent(ms),
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

            return response;
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