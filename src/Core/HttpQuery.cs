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
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Reactive.Threading.Tasks;
    using System.Threading.Tasks;
    using Html;
    using Mannex.Collections.Generic;
    using Mannex.Collections.Specialized;
    using Mannex.Web;
    using Mime;

    #endregion

    public static class HttpQuery
    {
        static readonly int MaximumAutomaticRedirections = WebRequest.CreateHttp("http://localhost/").MaximumAutomaticRedirections;

        static async Task<(IHttpClient Client, HttpFetch Fetch, HttpContent Content)>
            SendAsync(IHttpClient http, HttpConfig config, int id, HttpMethod method, Uri url, HttpContent content = null, HttpOptions options = null)
        {
            http = http.WithConfig(config);

            for (var redirections = 0; ; redirections++)
            {
                if (redirections > MaximumAutomaticRedirections)
                    throw new Exception("The maximum number of redirection responses permitted has been exceeded.");

                var result =
                    await HttpFetchAsync(http, http.Config, method, url, content, options,
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
                        })
                        .DontContinueOnCapturedContext();

                if (result.Response != null)
                    return (http.WithConfig(result.Config), result.Response.ToHttpFetch(id), result.Response.Content);

                // TODO tail call recursion?

                http    = http.WithConfig(result.Config);
                method  = result.Method;
                url     = result.Url;
                content = result.Content;
            }
        }

        static async Task<T> HttpFetchAsync<T>(IHttpClient http, HttpConfig config,
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

            HttpResponseMessage response = null;

            try
            {
                response = await http.SendAsync(request, config)
                    .DontContinueOnCapturedContext();

                if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
                {
                    var cc = new CookieContainer();
                    foreach (var cookie in setCookies)
                    {
                        try { cc.SetCookies(url, cookie); }
                        catch (CookieException) { /* ignore bad cookies */}
                    }

                    var mergedCookies =
                        from cookies in new[]
                        {
                            http.Config.Cookies ?? Enumerable.Empty<Cookie>(),
                            cc.GetCookies(url).Cast<Cookie>(),
                        }
                        from c in cookies
                        //
                        // According to RFC 6265[1], "cookies for a given host
                        // are shared across all the ports on that host" so
                        // don't take Cookie.Port into account when grouping.
                        // It is also assumed that Cookie.Domain
                        //
                        // [1] https://tools.ietf.org/html/rfc6265#section-1
                        //
                        group c by new
                        {
                            c.Name,
                            Domain = c.Domain.ToLowerInvariant(),
                            c.Path
                        } into g
                        select g.OrderByDescending(e => e.TimeStamp).First();

                    config = config.WithCookies(mergedCookies.ToArray());
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

                if (!options.ReturnErroneousFetch)
                    response.EnsureSuccessStatusCode();

                var result = responseSelector(config, response);
                response = null; // disown
                return result;
            }
            finally
            {
                response?.Dispose();
            }
        }

        public static IHttpObservable Get(this IHttpObservable query, Uri url) =>
            HttpObservable.Return(
                from first in query
                select first.Client.Get(url));

        public static IHttpObservable Get(this IHttpClient http, Uri url) =>
            HttpObservable.Return(ho =>
                // TODO Use DeferAsync
                Observable.Defer(() =>
                    SendAsync(http, ho.Configurer(http.Config), 0, HttpMethod.Get, url, options: ho.Options)
                        .ToObservable()
                        .Select(f => f.Fetch.And(f.Client, f.Content).WithConfig(http.Config.WithCookies(f.Client.Config.Cookies)))));

        public static IHttpObservable Post(this IHttpObservable query, Uri url, NameValueCollection data) =>
            HttpObservable.Return(
                from f in query
                select f.Client.Post(url, data));

        public static IHttpObservable Post(this IHttpObservable query, Uri url, HttpContent content) =>
            HttpObservable.Return(
                from f in query
                select f.Client.Post(url, content));

        public static IHttpObservable Post(this IHttpClient http, Uri url, NameValueCollection data) =>
            http.Post(url, new FormUrlEncodedContent(from i in Enumerable.Range(0, data.Count)
                                                     from v in data.GetValues(i)
                                                     select data.GetKey(i).AsKeyTo(v)));

        public static IHttpObservable Post(this IHttpClient http, Uri url, HttpContent content) =>
            HttpObservable.Return(ho =>
                // TODO Use DeferAsync
                Observable.Defer(() =>
                    SendAsync(http, ho.Configurer(http.Config), 0, HttpMethod.Post, url, content, ho.Options)
                        .ToObservable()
                        .Select(f => f.Fetch.And(f.Client, f.Content).WithConfig(http.Config.WithCookies(f.Client.Config.Cookies)))));

        public static IObservable<HttpFetch<T>> WithTimeout<T>(this IObservable<HttpFetch<T>> query, TimeSpan duration) =>
            from e in query
            select e.WithConfig(e.Client.Config.WithTimeout(duration));

        public static IObservable<HttpFetch<T>> WithUserAgent<T>(this IObservable<HttpFetch<T>> query, string ua) =>
            from e in query
            select e.WithConfig(e.Client.Config.WithUserAgent(ua));

        public static IObservable<TResult> Content<TContent, TResult>(this IObservable<HttpFetch<TContent>> query, Func<IHttpClient, TContent, TResult> selector) =>
            from e in query select selector(e.Client, e.Content);

        public static IObservable<T> Content<T>(this IObservable<HttpFetch<T>> query) =>
            from e in query select e.Content;

        public static IHttpObservable Accept(this IHttpObservable query, params string[] mediaTypes) =>
            (mediaTypes?.Length ?? 0) == 0
            ? query
            : query.Do(e =>
              {
                  var c = new StringContent(string.Empty) { Headers = { ContentType = null } };
                  foreach (var h in e.ContentHeaders)
                      c.Headers.TryAddWithoutValidation(h.Key, h.Value);
                  var headers = c.Headers;
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

        public static IHttpObservable Submit(this IObservable<HttpFetch<ParsedHtml>> query, string formSelector, NameValueCollection data) =>
            Submit(query, formSelector, null, null, data);

        public static IHttpObservable Submit(this IObservable<HttpFetch<ParsedHtml>> query, int formIndex, NameValueCollection data) =>
            Submit(query, null, formIndex, null, data);

        public static IHttpObservable SubmitTo(this IObservable<HttpFetch<ParsedHtml>> query, Uri url, string formSelector, NameValueCollection data) =>
            Submit(query, formSelector, null, url, data);

        public static IHttpObservable SubmitTo(this IObservable<HttpFetch<ParsedHtml>> query, Uri url, int formIndex, NameValueCollection data) =>
            Submit(query, null, formIndex, url, data);

        internal static IHttpObservable Submit(this IObservable<HttpFetch<ParsedHtml>> query, string formSelector, int? formIndex, Uri url, ISubmissionData<Unit> data) =>
            HttpObservable.Return(
                from html in query
                select Submit(html.Client, html.Content, formSelector, formIndex, url, _ => data));

        public static IHttpObservable Submit(this IHttpClient http, ParsedHtml html, string formSelector, ISubmissionData<Unit> data) =>
            Submit(http, html, formSelector, null, null, _ => data);

        public static IHttpObservable Submit(this IHttpClient http, ParsedHtml html, int formIndex, ISubmissionData<Unit> data) =>
            Submit(http, html, null, formIndex, null, _ => data);

        public static IHttpObservable SubmitTo(this IHttpClient http, Uri url, ParsedHtml html, string formSelector, ISubmissionData<Unit> data) =>
            Submit(http, html, formSelector, null, url, _ => data);

        public static IHttpObservable SubmitTo(this IHttpClient http, Uri url, ParsedHtml html, int formIndex, ISubmissionData<Unit> data) =>
            Submit(http, html, null, formIndex, url, _ => data);

        public static IHttpObservable Submit(this IHttpClient http, ParsedHtml html, string formSelector, Func<HtmlForm, ISubmissionData<Unit>> data) =>
            Submit(http, html, formSelector, null, null, data);

        public static IHttpObservable Submit(this IHttpClient http, ParsedHtml html, int formIndex, Func<HtmlForm, ISubmissionData<Unit>> data) =>
            Submit(http, html, null, formIndex, null, data);

        public static IHttpObservable SubmitTo(this IHttpClient http, Uri url, ParsedHtml html, string formSelector, Func<HtmlForm, ISubmissionData<Unit>> data) =>
            Submit(http, html, formSelector, null, url, data);

        public static IHttpObservable SubmitTo(this IHttpClient http, Uri url, ParsedHtml html, int formIndex, Func<HtmlForm, ISubmissionData<Unit>> data) =>
            Submit(http, html, null, formIndex, url, data);

        static IHttpObservable Submit(IObservable<HttpFetch<ParsedHtml>> query, string formSelector, int? formIndex, Uri url, NameValueCollection data) =>
            HttpObservable.Return(
                from html in query
                select Submit(html.Client, html.Content, formSelector, formIndex, url, data));

        public static IHttpObservable Submit(this IHttpClient http, ParsedHtml html, string formSelector, NameValueCollection data) =>
            Submit(http, html, formSelector, null, null, data);

        public static IHttpObservable Submit(this IHttpClient http, ParsedHtml html, int formIndex, NameValueCollection data) =>
            Submit(http, html, null, formIndex, null, data);

        public static IHttpObservable SubmitTo(this IHttpClient http, Uri url, ParsedHtml html, string formSelector, NameValueCollection data) =>
            Submit(http, html, formSelector, null, url, data);

        public static IHttpObservable SubmitTo(this IHttpClient http, Uri url, ParsedHtml html, int formIndex, NameValueCollection data) =>
            Submit(http, html, null, formIndex, url, data);

        internal static IHttpObservable Submit(IHttpClient http,
            ParsedHtml html,
            string formSelector, int? formIndex, Uri actionUrl,
            NameValueCollection data)
        {
            var submission = SubmissionData.Return(Unit.Default);

            if (data != null)
            {
                foreach (var e in data.AsEnumerable())
                {
                    submission = submission.Do(fsc => fsc.Remove(e.Key));
                    if (e.Value.Length == 1 && e.Value[0] == null)
                        continue;
                    submission = e.Value.Aggregate(submission, (current, value) => current.Do(fsc => fsc.Add(e.Key, value)));
                }
            }

            return Submit(http, html, formSelector, formIndex, actionUrl, _ => submission);
        }

        internal static IHttpObservable Submit<T>(IHttpClient http,
            ParsedHtml html,
            string formSelector, int? formIndex, Uri actionUrl,
            Func<HtmlForm, ISubmissionData<T>> submissions)
        {
            var forms =
                from f in formIndex == null
                          ? html.QueryFormSelectorAll(formSelector)
                          : formIndex < html.Forms.Count
                          ? Enumerable.Repeat(html.Forms[(int) formIndex], 1)
                          : Enumerable.Empty<HtmlForm>()
                select new
                {
                    Object = f,
                    Action = new Uri(html.TryBaseHref(f.Action), UriKind.Absolute),
                    // f.EncType, // TODO validate
                    Data = f.GetSubmissionData(),
                };

            var form = forms.FirstOrDefault();
            if (form == null)
                throw new Exception("No HTML form for submit.");

            submissions(form.Object).Run(form.Data);

            return form.Object.Method == HtmlFormMethod.Post
                 ? http.Post(actionUrl ?? form.Action, form.Data)
                 : http.Get(new UriBuilder(actionUrl ?? form.Action) { Query = form.Data.ToW3FormEncoded() }.Uri);
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
