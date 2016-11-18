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
    using Html;
    using Mannex.Collections.Generic;
    using Mannex.Collections.Specialized;
    using Mannex.Web;
    using Mime;
    using TryParsers;

    #endregion

    public static class HttpQuery
    {
        public static HttpRequestBuilder<Unit> Http => new HttpRequestBuilder<Unit>();
        public static HttpRequestBuilder<Unit> HttpWith(HttpConfig config) => new HttpRequestBuilder<Unit>(config);

        public static IEnumerable<T> Content<T>(this IEnumerable<HttpFetch<T>> query) =>
            from e in query select e.Content;

        public static IEnumerable<HttpFetch<HttpContent>> Accept(this IEnumerable<HttpFetch<HttpContent>> query, params string[] mediaTypes) =>
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

        public static IEnumerable<HttpFetch<HttpContent>> Submit(this IEnumerable<HttpFetch<HttpContent>> query, HttpConfig config, string formSelector, NameValueCollection data) =>
            Submit(query, formSelector, null, data, config);

        public static IEnumerable<HttpFetch<HttpContent>> Submit(this IEnumerable<HttpFetch<HttpContent>> query, string formSelector, NameValueCollection data) =>
            Submit(query, formSelector, null, data);

        public static IEnumerable<HttpFetch<HttpContent>> Submit(this IEnumerable<HttpFetch<HttpContent>> query, int formIndex, NameValueCollection data) =>
            Submit(query, null, formIndex, data);

        static IEnumerable<HttpFetch<HttpContent>> Submit(this IEnumerable<HttpFetch<HttpContent>> query, string formSelector, int? formIndex, NameValueCollection data, HttpConfig config = null) =>
            from html in query.Html()
            from fetch in Submit(html.Content, formSelector, formIndex, data, config)
            select fetch;

        public static IEnumerable<HttpFetch<HttpContent>> Submit(ParsedHtml html, string formSelector, NameValueCollection data) =>
            Submit(html, formSelector, null, data);

        public static IEnumerable<HttpFetch<HttpContent>> Submit(ParsedHtml html, int formIndex, NameValueCollection data) =>
            Submit(html, null, formIndex, data);

        static IEnumerable<HttpFetch<HttpContent>> Submit(ParsedHtml html,
                                                    string formSelector, int? formIndex,
                                                    NameValueCollection data,
                                                    HttpConfig config = null)
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
                 ? Http.Post(config, form.Action, form.Data)
                 : Http.Get(config, new UriBuilder(form.Action) { Query = form.Data.ToW3FormEncoded() }.Uri);
        }

        public static IEnumerable<HttpFetch<T>> ExceptStatusCode<T>(this IEnumerable<HttpFetch<T>> query, params HttpStatusCode[] statusCodes) =>
            query.Do(e =>
            {
                if (e.IsSuccessStatusCode || statusCodes.Any(sc => e.StatusCode == sc))
                    return;
                (e.Content as IDisposable)?.Dispose();
                throw new HttpRequestException($"Response status code does not indicate success: {e.StatusCode}.");
            });

        public static IEnumerable<HttpFetch<HttpContent>> Crawl(Uri url) =>
            Crawl(url, int.MaxValue);

        public static IEnumerable<HttpFetch<HttpContent>> Crawl(Uri url, int depth) =>
            Crawl(url, depth, _ => true);

        public static IEnumerable<HttpFetch<HttpContent>> Crawl(Uri url, Func<Uri, bool> followPredicate) =>
            Crawl(url, int.MaxValue, followPredicate);

        public static IEnumerable<HttpFetch<HttpContent>> Crawl(Uri url, int depth, Func<Uri, bool> followPredicate) =>
            CrawlImpl(url, depth, followPredicate);

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
                var fetch = Http.ReturnErrorneousFetch().Get(url).Single();

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
                    from e in fetch.ToSeq().Links().Content()
                    select TryParse.Uri(e, UriKind.Absolute) into e
                    where e != null
                       && (e.Scheme == Uri.UriSchemeHttp || e.Scheme == Uri.UriSchemeHttps)
                       && !linkSet.Contains(e)
                       && rootUrl.Host.Equals(e.Host, StringComparison.OrdinalIgnoreCase)
                       && followPredicate(e)
                    select e;

                foreach (var e in lq)
                {
                    if (linkSet.Add(e))
                        queue.Enqueue((level + 1).AsKeyTo(e));
                }
            }
        }
    }
}
