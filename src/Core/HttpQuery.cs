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
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reactive.Linq;
    using Html;
    using Mannex.Collections.Generic;
    using Mannex.Collections.Specialized;
    using Mannex.Web;
    using Mime;

    #endregion


    public static class HttpQuery
    {
        static readonly int MaximumAutomaticRedirections = WebRequest.CreateHttp("http://localhost/").MaximumAutomaticRedirections;

        static HttpFetch<HttpContent> Send(IHttpClient<HttpConfig> http, HttpConfig config, int id, HttpMethod method, Uri url, HttpContent content = null, HttpOptions options = null)
        {
            http = http.WithConfig(config);

            for (var redirections = 0; ; redirections++)
            {
                if (redirections > MaximumAutomaticRedirections)
                    throw new Exception("The maximum number of redirection responses permitted has been exceeded.");

                var result =
                    HttpFetch(http, http.Config, method, url, content, options,
                        (cfg, rsp) => new
                        {
                            Config   = cfg,
                            Method   = default(HttpMethod),
                            Url      = default(Uri),
                            Content  = default(HttpContent),
                            Response = rsp,
                        },
                        (cfg, rm, rl, rc) => new
                        {
                            Config   = cfg,
                            Method   = rm,
                            Url      = rl,
                            Content  = rc,
                            Response = default(HttpResponseMessage),
                        });

                if (result.Response != null)
                    return result.Response.ToHttpFetch(id, http.WithConfig(result.Config));

                // TODO tail call recursion?

                http    = http.WithConfig(result.Config);
                method  = result.Method;
                url     = result.Url;
                content = result.Content;
            }
        }

        static T HttpFetch<T>(IHttpClient<HttpConfig> http, HttpConfig config,
            HttpMethod method, Uri url, HttpContent content, HttpOptions options,
            Func<HttpConfig, HttpResponseMessage, T> responseSelector,
            Func<HttpConfig, HttpMethod, Uri, HttpContent, T> redirectionSelector)
        {
            var request = new HttpRequestMessage
            {
                Method = method,
                RequestUri = url,
                Content = content
            };

            var response = http.Send(request, config, options);
            IEnumerable<string> cookies;
            if (response.Headers.TryGetValues("Set-Cookie", out cookies))
            {
                var cc = new CookieContainer();
                foreach (var cookie in http.Config.Cookies ?? Enumerable.Empty<Cookie>())
                    cc.Add(cookie);
                foreach (var cookie in cookies)
                    cc.SetCookies(url, cookie);
                config = config.WithCookies(cc.GetCookies(url).Cast<Cookie>().ToArray());
            }

            // Source:
            // https://referencesource.microsoft.com/#System/net/System/Net/HttpWebRequest.cs,5669
            //
            // Check for Redirection
            //
            // Table View:
            // Method            301             302             303             307
            //    *                *               *             GET               *
            // POST              GET             GET             GET            POST
            //
            // Put another way:
            //  301 & 302  - All methods are redirected to the same method but POST. POST is redirected to a GET.
            //  303 - All methods are redirected to GET
            //  307 - All methods are redirected to the same method.
            //

            var sc = response.StatusCode;
            if (sc == HttpStatusCode.Ambiguous || // 300
                sc == HttpStatusCode.Moved || // 301
                sc == HttpStatusCode.Redirect || // 302
                sc == HttpStatusCode.RedirectMethod || // 303
                sc == HttpStatusCode.RedirectKeepVerb) // 307
            {
                var redirectionUrl = response.Headers.Location?.AsRelativeTo(response.RequestMessage.RequestUri);
                if (redirectionUrl == null)
                {
                    // 300
                    // If the server has a preferred choice of representation,
                    // it SHOULD include the specific URI for that
                    // representation in the Location field; user agents MAY
                    // use the Location field value for automatic redirection.

                    if (sc != HttpStatusCode.Ambiguous)
                        throw new ProtocolViolationException("Server did not supply a URL for a redirection response.");
                }
                else
                {
                    if (redirectionUrl.Scheme == "ws" || redirectionUrl.Scheme == "wss")
                        throw new NotSupportedException($"Redirection to a WebSocket URL ({redirectionUrl}) is not supported.");

                    if (redirectionUrl.Scheme != Uri.UriSchemeHttp && redirectionUrl.Scheme != Uri.UriSchemeHttps)
                        throw new ProtocolViolationException(
                            $"Server sent a redirection response where the redirection URL ({redirectionUrl}) scheme was neither HTTP nor HTTPS.");

                    return sc == HttpStatusCode.RedirectMethod
                        || method == HttpMethod.Post && (sc == HttpStatusCode.Moved || sc == HttpStatusCode.Redirect)
                         ? redirectionSelector(config, HttpMethod.Get, redirectionUrl, null)
                         : redirectionSelector(config, method, redirectionUrl, content);
                   
                }

            }

            return responseSelector(config, response);
        }

        public static IObservable<HttpFetch<HttpContent>> Get(
            this IObservable<HttpFetch<HttpContent>> query, Uri url) =>
            query.Get(url, null);

        public static IObservable<HttpFetch<HttpContent>> Get(
            this IObservable<HttpFetch<HttpContent>> query, Uri url, HttpOptions options) =>
            from first in query
            from second in first.Client.Get(url, options)
            select second;

        public static IObservable<HttpFetch<HttpContent>> Get(this IHttpClient<HttpConfig> http, Uri url) =>
            http.Get(url, null);

        public static IObservable<HttpFetch<HttpContent>> Get(this IHttpClient<HttpConfig> http, Uri url, HttpOptions options) =>
            Observable.Defer(() => Observable.Return(Send(http, http.Config, 0, HttpMethod.Get, url, options: options)));

        public static IObservable<HttpFetch<HttpContent>> Post(this IHttpClient<HttpConfig> http, Uri url, NameValueCollection data) =>
            http.Post(url, new FormUrlEncodedContent(from i in Enumerable.Range(0, data.Count)
                                                     from v in data.GetValues(i)
                                                     select data.GetKey(i).AsKeyTo(v)));

        public static IObservable<HttpFetch<HttpContent>> Post(this IHttpClient<HttpConfig> http, Uri url, HttpContent content) =>
            Observable.Defer(() => Observable.Return(Send(http, http.Config, 0, HttpMethod.Post, url, content)));

        public static IObservable<HttpFetch<T>> WithTimeout<T>(this IObservable<HttpFetch<T>> query, TimeSpan duration) =>
            from e in query
            select e.WithConfig(e.Client.Config.WithTimeout(duration));

        public static IObservable<HttpFetch<T>> WithUserAgent<T>(this IObservable<HttpFetch<T>> query, string ua) =>
            from e in query
            select e.WithConfig(e.Client.Config.WithUserAgent(ua));

        public static readonly IHttpClient<HttpConfig> Http = new HttpClient(HttpConfig.Default);

        public static IObservable<IHttpClient<HttpConfig>> HttpWithConfig(
            Func<HttpConfig, HttpConfig> configurator) =>
            Observable.Defer(() =>
            {
                IHttpClient<HttpConfig> http = new HttpClient(configurator(HttpConfig.Default));
                return Observable.Return(http);
            });

        public static IObservable<TResult> Content<TContent, TResult>(this IObservable<HttpFetch<TContent>> query, Func<IHttpClient<HttpConfig>, TContent, TResult> selector) =>
            from e in query select selector(e.Client, e.Content);

        public static IObservable<T> Content<T>(this IObservable<HttpFetch<T>> query) =>
            from e in query select e.Content;

        public static IObservable<HttpFetch<HttpContent>> Accept(this IObservable<HttpFetch<HttpContent>> query, params string[] mediaTypes) =>
            (mediaTypes?.Length ?? 0) == 0
            ? query
            : query.Do(e =>
            {
                var headers = e.Content.Headers;
                var actualMediaType = headers.ContentType?.MediaType;
                if (actualMediaType == null)
                {
                    var contentDisposition = headers.ContentDisposition;
                    var filename = contentDisposition?.FileName ?? contentDisposition?.FileNameStar;
                    if (!string.IsNullOrEmpty(filename))
                        actualMediaType = MimeMapping.FindMimeTypeFromFileName(filename);
                    if (actualMediaType == null)
                    {
                        throw new Exception($"Content has unspecified type when acceptable types are: {string.Join(", ", mediaTypes)}");
                    }
                }

                Debug.Assert(mediaTypes != null);
                if (mediaTypes.Any(mediaType => string.Equals(mediaType, actualMediaType, StringComparison.OrdinalIgnoreCase)))
                    return;

                throw new Exception($"Unexpected content of type \"{actualMediaType}\". Acceptable types are: {string.Join(", ", mediaTypes)}");
            });


        public static IObservable<HttpFetch<HttpContent>> Submit(this IObservable<HttpFetch<ParsedHtml>> query, string formSelector, NameValueCollection data) =>
            from html in query
            from next in html.Client.Submit(html.Content, formSelector, data)
            select next;

        public static IObservable<HttpFetch<HttpContent>> Submit(this IObservable<HttpFetch<ParsedHtml>> query, int formIndex, NameValueCollection data) =>
            from html in query
            from next in html.Client.Submit(html.Content, formIndex, data)
            select next;

        public static IObservable<HttpFetch<HttpContent>> Submit(this IHttpClient<HttpConfig> http, ParsedHtml html, string formSelector, NameValueCollection data) =>
            Submit(http, html, formSelector, null, data);

        public static IObservable<HttpFetch<HttpContent>> Submit(this IHttpClient<HttpConfig> http, ParsedHtml html, int formIndex, NameValueCollection data) =>
            Submit(http, html, null, formIndex, data);

        internal static IObservable<HttpFetch<HttpContent>> Submit(
            IHttpClient<HttpConfig> http, ParsedHtml html,
            string formSelector, int? formIndex,
            NameValueCollection data)
        {
            var forms =
                from f in formIndex == null
                          ? html.QueryFormSelectorAll(formSelector)
                          : formIndex < html.Forms.Count
                          ? Enumerable.Repeat(html.Forms[(int) formIndex], 1)
                          : Enumerable.Empty<HtmlForm>()
                select new
                {
                    Action = new Uri(html.TryBaseHref(f.Action), UriKind.Absolute),
                    f.Method,
                    f.EncType, // TODO validate
                    Data = f.GetSubmissionData(),
                };

            var form = forms.FirstOrDefault();
            if (form == null)
                throw new Exception("No HTML form for submit.");

            if (data != null)
            {
                foreach (var e in data.AsEnumerable())
                {
                    form.Data.Remove(e.Key);
                    if (e.Value.Length == 1 && e.Value[0] == null)
                        continue;
                    foreach (var value in e.Value)
                        form.Data.Add(e.Key, value);
                }
            }

            return form.Method == HtmlFormMethod.Post
                 ? http.Post(form.Action, form.Data)
                 : http.Get(new UriBuilder(form.Action) { Query = form.Data.ToW3FormEncoded() }.Uri);
        }

        public static IObservable<HttpFetch<T>> ExceptStatusCode<T>(this IObservable<HttpFetch<T>> query, params HttpStatusCode[] statusCodes) =>
            query.Do(e =>
            {
                if (e.IsSuccessStatusCode || statusCodes.Any(sc => e.StatusCode == sc))
                    return;
                (e.Content as IDisposable)?.Dispose();
                throw new HttpRequestException($"Response status code does not indicate success: {e.StatusCode}.");
            });

    }

    static class UriExtensions
    {
        public static Uri AsRelativeTo(this Uri uri, Uri baseUri)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            return uri.IsAbsoluteUri ? uri : new Uri(baseUri, uri);
        }
    }
}
