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
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using Html;
    using Mannex.Collections.Generic;
    using Sys;
    using Xsv;
    using Unit = System.Reactive.Unit;
    using HttpClient = HttpClient;
    using LoadOption = System.Xml.Linq.LoadOptions;

    #endregion

    public static class HttpModule
    {
        public static IHttpClient Http => HttpClient.Default;

        public static IObservable<HttpFetch<ParsedHtml>> Html(this IHttpObservable query) =>
            query.Html(HtmlParser.Default);

        public static IHttpObservable Submit(this IHttpObservable query, string formSelector, NameValueCollection data) =>
            Submit(query, formSelector, null, null, data);

        public static IHttpObservable Submit(this IHttpObservable query, int formIndex, NameValueCollection data) =>
            Submit(query, null, formIndex, null, data);

        public static IHttpObservable SubmitTo(this IHttpObservable query, Uri url, string formSelector, NameValueCollection data) =>
            Submit(query, formSelector, null, url, data);

        public static IHttpObservable SubmitTo(this IHttpObservable query, Uri url, int formIndex, NameValueCollection data) =>
            Submit(query, null, formIndex, url, data);

        static IHttpObservable Submit(IHttpObservable query, string formSelector, int? formIndex, Uri url, NameValueCollection data) =>
            HttpObservable.Return(
                from html in query.Html()
                select HttpQuery.Submit(html.Client, html.Content, formSelector, formIndex, url, data));

        public static IHttpObservable Submit(this IHttpObservable query, string formSelector, ISubmissionData<Unit> data) =>
            Submit(query, formSelector, null, null, data);

        public static IHttpObservable Submit(this IHttpObservable query, int formIndex, ISubmissionData<Unit> data) =>
            Submit(query, null, formIndex, null, data);

        public static IHttpObservable SubmitTo(this IHttpObservable query, Uri url, string formSelector, ISubmissionData<Unit> data) =>
            Submit(query, formSelector, null, url, data);

        public static IHttpObservable SubmitTo(this IHttpObservable query, Uri url, int formIndex, ISubmissionData<Unit> data) =>
            Submit(query, null, formIndex, url, data);

        static IHttpObservable Submit(IHttpObservable query, string formSelector, int? formIndex, Uri url, ISubmissionData<Unit> data) =>
            query.Html().Submit(formSelector, formIndex, url, data);

        public static IObservable<HttpFetch<string>> Links(this IHttpObservable query) =>
            query.Links(null);

        public static IObservable<HttpFetch<T>> Links<T>(this IHttpObservable query, Func<string, HtmlObject, T> selector) =>
            Links(query, null, selector);

        public static IObservable<HttpFetch<string>> Links(this IHttpObservable query, Uri baseUrl) =>
            Links(query, baseUrl, (href, _) => href);

        public static IObservable<HttpFetch<T>> Links<T>(this IHttpObservable query, Uri baseUrl, Func<string, HtmlObject, T> selector) =>
            query.Html().Links(baseUrl, selector);

        public static IObservable<HttpFetch<HtmlObject>> Tables(this IHttpObservable query) =>
            query.Html().Tables();

        public static IObservable<HttpFetch<DataTable>> FormsAsDataTable(this IHttpObservable query) =>
            query.Html().FormsAsDataTable();

        public static IObservable<HttpFetch<HtmlForm>> Forms(this IHttpObservable query) =>
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
                var fetch = Http.Get(url)
                                .SkipErroneousFetch()
                                .Buffer()
                                .SingleOrDefaultAsync()
                                .GetAwaiter()
                                .GetResult();

                if (fetch == null)
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
                    from e in HttpObservable.Return(_ => Observable.Return(fetch)).Links().Content()
                    select Uri.TryCreate(e, UriKind.Absolute, out url) ? url : null into e
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

    public static class XmlModule
    {
        public static XDocument ParseXml(string xml) =>
            XDocument.Parse(xml, LoadOptions.None);
        public static XDocument ParseXml(string xml, LoadOption options) =>
            XDocument.Parse(xml, options);
    }

    public static class SpawnModule
    {
        public static IObservable<string> Spawn(string path, ProgramArguments args) =>
            Spawner.Default.Spawn(path, args, null);

        public static IObservable<string> Spawn(string path, ProgramArguments args, string workingDirectory) =>
            Spawner.Default.Spawn(path, args, workingDirectory, output => output, null);

        public static IObservable<KeyValuePair<T, string>> Spawn<T>(string path, ProgramArguments args, T stdoutKey, T stderrKey) =>
            Spawner.Default.Spawn(path, args, null, stdoutKey, stderrKey);

        public static IObservable<KeyValuePair<T, string>> Spawn<T>(string path, ProgramArguments args, string workingDirectory, T stdoutKey, T stderrKey) =>
            Spawner.Default.Spawn(path, args, workingDirectory,
                                  stdout => stdoutKey.AsKeyTo(stdout),
                                  stderr => stderrKey.AsKeyTo(stderr));

        public static IObservable<T> Spawn<T>(string path, ProgramArguments args, Func<string, T> stdoutSelector, Func<string, T> stderrSelector) =>
            Spawner.Default.Spawn(path, args, null, stdoutSelector, stderrSelector);

        //

        [Obsolete("Use the " + nameof(Spawn) + " overload taking" + nameof(ProgramArguments) + " instead.")]
        public static IObservable<string> Spawn(string path, string args) =>
            Spawner.Default.Spawn(path, ProgramArguments.Parse(args), null);

        [Obsolete("Use the " + nameof(Spawn) + " overload taking" + nameof(ProgramArguments) + " instead.")]
        public static IObservable<string> Spawn(string path, string args, string workingDirectory) =>
            Spawner.Default.Spawn(path, ProgramArguments.Parse(args), workingDirectory, output => output, null);

        [Obsolete("Use the " + nameof(Spawn) + " overload taking" + nameof(ProgramArguments) + " instead.")]
        public static IObservable<KeyValuePair<T, string>> Spawn<T>(string path, string args, T stdoutKey, T stderrKey) =>
            Spawner.Default.Spawn(path, ProgramArguments.Parse(args), null, stdoutKey, stderrKey);

        [Obsolete("Use the " + nameof(Spawn) + " overload taking" + nameof(ProgramArguments) + " instead.")]
        public static IObservable<KeyValuePair<T, string>> Spawn<T>(string path, string args, string workingDirectory, T stdoutKey, T stderrKey) =>
            Spawner.Default.Spawn(path, ProgramArguments.Parse(args), workingDirectory,
                                  stdout => stdoutKey.AsKeyTo(stdout),
                                  stderr => stderrKey.AsKeyTo(stderr));

        [Obsolete("Use the " + nameof(Spawn) + " overload taking" + nameof(ProgramArguments) + " instead.")]
        public static IObservable<T> Spawn<T>(string path, string args, Func<string, T> stdoutSelector, Func<string, T> stderrSelector) =>
            Spawner.Default.Spawn(path, ProgramArguments.Parse(args), null, stdoutSelector, stderrSelector);
    }
}
