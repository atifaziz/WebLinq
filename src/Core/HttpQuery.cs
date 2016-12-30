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
    using TryParsers;

    #endregion

    public interface IHttpObservable<TConfig, out TResult> : IObservable<TResult>
    {
        IHttpClient<TConfig> HttpClient { get; }
    }

    static class HttpObservable
    {
        public static IHttpObservable<TConfig, TResult> WithHttpClient<TConfig, TResult>(this IObservable<TResult> results, IHttpClient<TConfig> httpClient) =>
            new Impl<TConfig, TResult>(httpClient, results);

        public static IHttpObservable<TConfig, TResult> WithConfig<TConfig, TResult>(this IHttpObservable<TConfig, TResult> results, TConfig config) =>
            new Reconfigured<TConfig, TResult>(results, config);

        sealed class Impl<TConfig, TResult> : IHttpObservable<TConfig, TResult>
        {
            readonly IObservable<TResult> _results;

            public Impl(IHttpClient<TConfig> httpClient, IObservable<TResult> results)
            {
                _results = results;
                HttpClient = httpClient;
            }

            public IDisposable Subscribe(IObserver<TResult> observer) =>
                _results.Subscribe(observer);

            public IHttpClient<TConfig> HttpClient { get; }
        }

        sealed class Reconfigured<TConfig, TResult> : IHttpObservable<TConfig, TResult>
        {
            readonly IHttpObservable<TConfig, TResult> _results;
            readonly TConfig _config;

            public Reconfigured(IHttpObservable<TConfig, TResult> results, TConfig config)
            {
                _results = results;
                _config = config;
            }

            public IDisposable Subscribe(IObserver<TResult> observer) =>
                _results.Subscribe(observer);

            public IHttpClient<TConfig> HttpClient =>
                _results.HttpClient.WithConfig(_config);
        }
    }

    public static class HttpQuery
    {
        public static IHttpObservable<TConfig, TResult> WithTimeout<TConfig, TResult>(
            this IHttpObservable<TConfig, TResult> source, TimeSpan duration)
            where TConfig : IHttpTimeoutOption<TConfig> =>
            source.WithConfig(source.HttpClient.Config.WithTimeout(duration));

        public static IHttpObservable<TConfig, TResult> WithUserAgent<TConfig, TResult>(
            this IHttpObservable<TConfig, TResult> source, string value)
            where TConfig : IHttpUserAgentOption<TConfig> =>
            source.WithConfig(source.HttpClient.Config.WithUserAgent(value));

        static HttpFetch<HttpContent> Send<T>(IHttpClient<T> http, T config, int id, HttpMethod method, Uri url, HttpContent content = null, HttpOptions options = null)
        {
            var request = new HttpRequestMessage
            {
                Method = method,
                RequestUri = url,
                Content = content
            };
            return http.Send(request, config, options).ToHttpFetch(id);
        }

        public static IHttpObservable<T, HttpFetch<HttpContent>> Get<T>(this IHttpObservable<T, HttpFetch<HttpContent>> http, Uri url)
        {
            var q =
                from _ in http
                from fetch in http.HttpClient.Get(url, null)
                select fetch;
            return q.WithHttpClient(http.HttpClient);
        }

        public static IHttpObservable<T, HttpFetch<HttpContent>> Get<T>(
                this IHttpObservable<T, HttpFetch<HttpContent>> http, Uri url, HttpOptions options) =>
            http.HttpClient.Get(url, options);

        public static IHttpObservable<T, HttpFetch<HttpContent>> Get<T>(this IHttpClient<T> http, Uri url) =>
            http.Get(url, null);

        public static IHttpObservable<T, HttpFetch<HttpContent>> Get<T>(this IHttpClient<T> http, Uri url, HttpOptions options) =>
                Observable.Defer(() => Observable.Return(Send(http, http.Config, 0, HttpMethod.Get, url, options: options)))
                          .WithHttpClient(http);

        public static IHttpObservable<T, HttpFetch<HttpContent>> Post<T>(this IHttpClient<T> http, Uri url, NameValueCollection data) =>
            http.Post(url, new FormUrlEncodedContent(from i in Enumerable.Range(0, data.Count)
                                                     from v in data.GetValues(i)
                                                     select data.GetKey(i).AsKeyTo(v)));

        public static IHttpObservable<T, HttpFetch<HttpContent>> Post<T>(this IHttpClient<T> http, Uri url, HttpContent content) =>
                Observable.Defer(() => Observable.Return(Send(http, http.Config, 0, HttpMethod.Post, url, content)))
                          .WithHttpClient(http);

        public static IHttpClientObservable<T> WithTimeout<T>(this IHttpClientObservable<T> client, TimeSpan duration)
            where T : IHttpTimeoutOption<T> =>
                client.WithConfig(client.Config.WithTimeout(duration));

        public static IHttpClientObservable<T> WithUserAgent<T>(this IHttpClientObservable<T> client, string ua)
            where T : IHttpUserAgentOption<T> =>
                client.WithConfig(client.Config.WithUserAgent(ua));

        public static IHttpObservable<HttpConfig, IHttpClient<HttpConfig>> Http
        {
            get
            {
                var http = new HttpClient(HttpConfig.Default);
                return Observable.Return(http).WithHttpClient(http);
            }
        }

        public static IObservable<IHttpClient<HttpConfig>> HttpWithConfig(
            Func<HttpConfig, HttpConfig> configurator) =>
            Observable.Defer(() =>
            {
                IHttpClient<HttpConfig> http = new HttpClient(configurator(HttpConfig.Default));
                return Observable.Return(http);
            });

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

        public static IHttpObservable<T, HttpFetch<HttpContent>> Submit<T>(this IHttpObservable<T, HttpFetch<HttpContent>> query, string formSelector, NameValueCollection data) =>
            query.Submit(query.HttpClient, formSelector, data);

        public static IHttpObservable<T, HttpFetch<HttpContent>> Submit<T>(this IHttpObservable<T, HttpFetch<HttpContent>> query, int formIndex, NameValueCollection data) =>
            query.Submit(query.HttpClient, formIndex, data);

        public static IHttpObservable<T, HttpFetch<HttpContent>> Submit<T>(this IObservable<HttpFetch<HttpContent>> query, IHttpClient<T> http, string formSelector, NameValueCollection data) =>
            query.Submit(http, formSelector, null, data);

        public static IHttpObservable<T, HttpFetch<HttpContent>> Submit<T>(this IObservable<HttpFetch<HttpContent>> query, IHttpClient<T> http, int formIndex, NameValueCollection data) =>
            query.Submit(http, null, formIndex, data);

        static IHttpObservable<T, HttpFetch<HttpContent>> Submit<T>(this IObservable<HttpFetch<HttpContent>> query, IHttpClient<T> http, string formSelector, int? formIndex, NameValueCollection data)
        {
            var q =
                from html in query.Html()
                from fetch in Submit(http, html.Content, formSelector, formIndex, data)
                select fetch;
            return q.WithHttpClient(http);
        }

        public static IHttpObservable<T, HttpFetch<HttpContent>> Submit<T>(this IHttpClient<T> http, ParsedHtml html, string formSelector, NameValueCollection data) =>
            Submit(http, html, formSelector, null, data);

        public static IHttpObservable<T, HttpFetch<HttpContent>> Submit<T>(this IHttpClient<T> http, ParsedHtml html, int formIndex, NameValueCollection data) =>
            Submit(http, html, null, formIndex, data);

        static IHttpObservable<T, HttpFetch<HttpContent>> Submit<T>(IHttpClient<T> http, ParsedHtml html,
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

        public static IObservable<HttpFetch<HttpContent>> Crawl(Uri url) =>
            Crawl(url, int.MaxValue);

        public static IObservable<HttpFetch<HttpContent>> Crawl(Uri url, int depth) =>
            Crawl(url, depth, _ => true);

        public static IObservable<HttpFetch<HttpContent>> Crawl(Uri url, Func<Uri, bool> followPredicate) =>
            Crawl(url, int.MaxValue, followPredicate);

        public static IObservable<HttpFetch<HttpContent>> Crawl(Uri url, int depth, Func<Uri, bool> followPredicate) =>
            CrawlImpl(url, depth, followPredicate).ToObservable();

        static IEnumerable<HttpFetch<HttpContent>> CrawlImpl(Uri rootUrl, int depth, Func<Uri, bool> followPredicate)
        {
            var linkSet = new HashSet<Uri> { rootUrl };
            var queue = new Queue<KeyValuePair<int, Uri>>();
            queue.Enqueue(0.AsKeyTo(rootUrl));

            while (queue.Count > 0)
            {
                var dequeued = queue.Dequeue();
                var url = dequeued.Value;
                var level = dequeued.Key;
                // TODO retry intermittent errors?
                var fetch = Http.SelectMany(http => http.Get(url, new HttpOptions {ReturnErrorneousFetch = true})).Single();

                if (!fetch.IsSuccessStatusCode)
                    continue;

                yield return fetch;

                if (level >= depth)
                    continue;

                // If content is HTML then sniff links and add them to the
                // queue assuming they are from the same domain and pass the
                // user-supplied condition to follow.

                var contentMediaType = fetch.Content.Headers.ContentType?.MediaType;
                if (!"text/html".Equals(contentMediaType, StringComparison.OrdinalIgnoreCase))
                    continue;

                var lq =
                    from e in Observable.Return(fetch).Links().Content()
                    select TryParse.Uri(e, UriKind.Absolute) into e
                    where e != null
                       && (e.Scheme == Uri.UriSchemeHttp || e.Scheme == Uri.UriSchemeHttps)
                       && !linkSet.Contains(e)
                       && rootUrl.Host.Equals(e.Host, StringComparison.OrdinalIgnoreCase)
                       && followPredicate(e)
                    select e;

                foreach (var e in lq.ToEnumerable())
                {
                    if (linkSet.Add(e))
                        queue.Enqueue((level + 1).AsKeyTo(e));
                }
            }
        }
    }
}
