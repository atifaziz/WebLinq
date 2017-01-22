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

namespace WebLinq.Modules
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.Linq;
    using System.Net.Http;
    using System.Reactive.Linq;
    using System.Xml.Linq;
    using System.Xml.XPath;
    using Html;
    using Mannex.Collections.Generic;
    using Sys;
    using TryParsers;
    using Xsv;
    using HttpClient = HttpClient;
    using LoadOption = System.Xml.Linq.LoadOptions;

    #endregion

    public static class HttpModule
    {
        public static IHttpClient<HttpConfig> Http => new HttpClient(HttpConfig.Default);

        public static IObservable<HttpFetch<ParsedHtml>> Html(this IObservable<HttpFetch<HttpContent>> query) =>
            query.Html(HtmlParser.Default);

        public static IObservable<HttpFetch<HttpContent>> Submit(this IObservable<HttpFetch<HttpContent>> query, string formSelector, NameValueCollection data) =>
            Submit(query, formSelector, null, data);

        public static IObservable<HttpFetch<HttpContent>> Submit(this IObservable<HttpFetch<HttpContent>> query, int formIndex, NameValueCollection data) =>
            Submit(query, null, formIndex, data);

        static IObservable<HttpFetch<HttpContent>> Submit(IObservable<HttpFetch<HttpContent>> query, string formSelector, int? formIndex, NameValueCollection data) =>
            from html in query.Html()
            from fetch in HttpQuery.Submit(html.Client, html.Content, formSelector, formIndex, data)
            select fetch;

        public static IObservable<HttpFetch<string>> Links(this IObservable<HttpFetch<HttpContent>> query) =>
            query.Links(null);

        public static IObservable<HttpFetch<T>> Links<T>(this IObservable<HttpFetch<HttpContent>> query, Func<string, HtmlObject, T> selector) =>
            Links(query, null, selector);

        public static IObservable<HttpFetch<string>> Links(this IObservable<HttpFetch<HttpContent>> query, Uri baseUrl) =>
            Links(query, baseUrl, (href, _) => href);

        public static IObservable<HttpFetch<T>> Links<T>(this IObservable<HttpFetch<HttpContent>> query, Uri baseUrl, Func<string, HtmlObject, T> selector) =>
            query.Html().Links(baseUrl, selector);

        public static IObservable<HttpFetch<HtmlObject>> Tables(this IObservable<HttpFetch<HttpContent>> query) =>
            query.Html().Tables();

        public static IObservable<HttpFetch<DataTable>> FormsAsDataTable(this IObservable<HttpFetch<HttpContent>> query) =>
            query.Html().FormsAsDataTable();

        public static IObservable<HttpFetch<HtmlForm>> Forms(this IObservable<HttpFetch<HttpContent>> query) =>
            query.Html().Forms();
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
                var fetch = Http.Get(url, new HttpOptions {ReturnErrorneousFetch = true}).Single();

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

    public static class HtmlModule
    {
        public static IHtmlParser HtmlParser => Html.HtmlParser.Default;

        public static ParsedHtml ParseHtml(string html) =>
            ParseHtml(html, null);

        public static ParsedHtml ParseHtml(string html, Uri baseUrl) =>
            HtmlParser.Parse(html, baseUrl);

        public static IEnumerable<string> Links(string html) =>
            Links(html, null, (href, _) => href);

        public static IEnumerable<T> Links<T>(string html, Func<string, HtmlObject, T> selector) =>
            Links(html, null, selector);

        public static IEnumerable<string> Links(string html, Uri baseUrl) =>
            Links(html, baseUrl, (href, _) => href);

        public static IEnumerable<T> Links<T>(string html, Uri baseUrl, Func<string, HtmlObject, T> selector) =>
            ParseHtml(html, baseUrl).Links(selector);

        public static IEnumerable<HtmlObject> Tables(string html) =>
            ParseHtml(html).Tables();
    }

    public static class XsvModule
    {
        public static IObservable<DataTable> CsvToDataTable(string text, params DataColumn[] columns) =>
            XsvToDataTable(text, ",", true, columns);

        public static IObservable<DataTable> XsvToDataTable(string text, string delimiter, bool quoted, params DataColumn[] columns) =>
            XsvQuery.XsvToDataTable(text, delimiter, quoted, columns);
    }

    public static partial class XmlModule
    {
        public static XDocument ParseXml(string xml) =>
            XDocument.Parse(xml, LoadOptions.None);
        public static XDocument ParseXml(string xml, LoadOption options) =>
            XDocument.Parse(xml, options);

        public static IEnumerable<TResult> Xml<TNode, TResult>(string xml, string xpath, Func<TNode, TResult> resultSelector)
        {
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            return Xml(xml, xpath, 1,
                        ".", resultSelector,
                        null, default(Func<object, object>),
                        null, default(Func<object, object>),
                        null, default(Func<object, object>),
                        null, default(Func<object, object>),
                        null, default(Func<object, object>),
                        null, default(Func<object, object>),
                        null, default(Func<object, object>),
                        null, default(Func<object, object>),
                        null, default(Func<object, object>),
                        null, default(Func<object, object>),
                        null, default(Func<object, object>),
                        null, default(Func<object, object>),
                        null, default(Func<object, object>),
                        null, default(Func<object, object>),
                        null, default(Func<object, object>),
                        (a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p) => a);
        }

        static IEnumerable<TResult> Xml<TNode1,  T1,
                                        TNode2,  T2,
                                        TNode3,  T3,
                                        TNode4,  T4,
                                        TNode5,  T5,
                                        TNode6,  T6,
                                        TNode7,  T7,
                                        TNode8,  T8,
                                        TNode9,  T9,
                                        TNode10, T10,
                                        TNode11, T11,
                                        TNode12, T12,
                                        TNode13, T13,
                                        TNode14, T14,
                                        TNode15, T15,
                                        TNode16, T16,
                                        TResult>(
            string xml,
            string xpath,
            int arity,
            string xpath1,  Func<TNode1,  T1>  selector1,
            string xpath2,  Func<TNode2,  T2>  selector2,
            string xpath3,  Func<TNode3,  T3>  selector3,
            string xpath4,  Func<TNode4,  T4>  selector4,
            string xpath5,  Func<TNode5,  T5>  selector5,
            string xpath6,  Func<TNode6,  T6>  selector6,
            string xpath7,  Func<TNode7,  T7>  selector7,
            string xpath8,  Func<TNode8,  T8>  selector8,
            string xpath9,  Func<TNode9,  T9>  selector9,
            string xpath10, Func<TNode10, T10> selector10,
            string xpath11, Func<TNode11, T11> selector11,
            string xpath12, Func<TNode12, T12> selector12,
            string xpath13, Func<TNode13, T13> selector13,
            string xpath14, Func<TNode14, T14> selector14,
            string xpath15, Func<TNode15, T15> selector15,
            string xpath16, Func<TNode16, T16> selector16,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult> resultSelector)
        {
            if (xml == null) throw new ArgumentNullException(nameof(xml));
            if (xpath == null) throw new ArgumentNullException(nameof(xpath));

            if (arity >= 2 ) { if (xpath2  == null) throw new ArgumentNullException(nameof(xpath2 )); if (selector2  == null) throw new ArgumentNullException(nameof(selector2 )); }
            if (arity >= 3 ) { if (xpath3  == null) throw new ArgumentNullException(nameof(xpath3 )); if (selector3  == null) throw new ArgumentNullException(nameof(selector3 )); }
            if (arity >= 4 ) { if (xpath4  == null) throw new ArgumentNullException(nameof(xpath4 )); if (selector4  == null) throw new ArgumentNullException(nameof(selector4 )); }
            if (arity >= 5 ) { if (xpath5  == null) throw new ArgumentNullException(nameof(xpath5 )); if (selector5  == null) throw new ArgumentNullException(nameof(selector5 )); }
            if (arity >= 6 ) { if (xpath6  == null) throw new ArgumentNullException(nameof(xpath6 )); if (selector6  == null) throw new ArgumentNullException(nameof(selector6 )); }
            if (arity >= 7 ) { if (xpath7  == null) throw new ArgumentNullException(nameof(xpath7 )); if (selector7  == null) throw new ArgumentNullException(nameof(selector7 )); }
            if (arity >= 8 ) { if (xpath8  == null) throw new ArgumentNullException(nameof(xpath8 )); if (selector8  == null) throw new ArgumentNullException(nameof(selector8 )); }
            if (arity >= 9 ) { if (xpath9  == null) throw new ArgumentNullException(nameof(xpath9 )); if (selector9  == null) throw new ArgumentNullException(nameof(selector9 )); }
            if (arity >= 10) { if (xpath10 == null) throw new ArgumentNullException(nameof(xpath10)); if (selector10 == null) throw new ArgumentNullException(nameof(selector10)); }
            if (arity >= 11) { if (xpath11 == null) throw new ArgumentNullException(nameof(xpath11)); if (selector11 == null) throw new ArgumentNullException(nameof(selector11)); }
            if (arity >= 12) { if (xpath12 == null) throw new ArgumentNullException(nameof(xpath12)); if (selector12 == null) throw new ArgumentNullException(nameof(selector12)); }
            if (arity >= 13) { if (xpath13 == null) throw new ArgumentNullException(nameof(xpath13)); if (selector13 == null) throw new ArgumentNullException(nameof(selector13)); }
            if (arity >= 14) { if (xpath14 == null) throw new ArgumentNullException(nameof(xpath14)); if (selector14 == null) throw new ArgumentNullException(nameof(selector14)); }
            if (arity >= 15) { if (xpath15 == null) throw new ArgumentNullException(nameof(xpath15)); if (selector15 == null) throw new ArgumentNullException(nameof(selector15)); }
            if (arity >= 16) { if (xpath16 == null) throw new ArgumentNullException(nameof(xpath16)); if (selector16 == null) throw new ArgumentNullException(nameof(selector16)); }

            return
                from e in XDocument.Parse(xml).XPathSelectElements(xpath)
                select
                resultSelector(
                    XPathEvaluate(e, xpath1, selector1),
                    XPathEvaluate(e, xpath2, selector2),
                    XPathEvaluate(e, xpath3, selector3),
                    XPathEvaluate(e, xpath4, selector4),
                    XPathEvaluate(e, xpath5, selector5),
                    XPathEvaluate(e, xpath6, selector6),
                    XPathEvaluate(e, xpath7, selector7),
                    XPathEvaluate(e, xpath8, selector8),
                    XPathEvaluate(e, xpath9, selector9),
                    XPathEvaluate(e, xpath10, selector10),
                    XPathEvaluate(e, xpath11, selector11),
                    XPathEvaluate(e, xpath12, selector12),
                    XPathEvaluate(e, xpath13, selector13),
                    XPathEvaluate(e, xpath14, selector14),
                    XPathEvaluate(e, xpath15, selector15),
                    XPathEvaluate(e, xpath16, selector16));
        }

        static TResult XPathEvaluate<TNode, TResult>(this XNode e, string xpath, Func<TNode, TResult> selector) =>
            selector != null && xpath != null
            ? selector((TNode)((IEnumerable<object>)e.XPathEvaluate(xpath)).FirstOrDefault())
            : default(TResult);
    }

    public static class SpawnModule
    {
        public static IObservable<string> Spawn(string path, string args) =>
            Spawner.Default.Spawn(path, args, null);

        public static IObservable<string> Spawn(string path, string args, string workingDirectory) =>
            Spawner.Default.Spawn(path, args, workingDirectory, output => output, null);

        public static IObservable<KeyValuePair<T, string>> Spawn<T>(string path, string args, T stdoutKey, T stderrKey) =>
            Spawner.Default.Spawn(path, args, null, stdoutKey, stderrKey);

        public static IObservable<KeyValuePair<T, string>> Spawn<T>(string path, string args, string workingDirectory, T stdoutKey, T stderrKey) =>
            Spawner.Default.Spawn(path, args, workingDirectory,
                                  stdout => stdoutKey.AsKeyTo(stdout),
                                  stderr => stderrKey.AsKeyTo(stderr));

        public static IObservable<T> Spawn<T>(string path, string args, Func<string, T> stdoutSelector, Func<string, T> stderrSelector) =>
            Spawner.Default.Spawn(path, args, null, stdoutSelector, stderrSelector);
    }
}
