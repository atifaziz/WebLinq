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

namespace WebLinq.Html
{
    using System;
    using System.Data;
    using System.Net.Mime;
    using System.Reactive.Linq;

    public static class HtmlQuery
    {
        public static IObservable<HttpFetch<ParsedHtml>> Html(this IHttpObservable query, IHtmlParser parser) =>
            query.Accept(MediaTypeNames.Text.Html)
                 .WithReader(async fetch => parser.Parse(await fetch.Content.ReadAsStringAsync()
                                                                            .DontContinueOnCapturedContext(),
                                                         fetch.RequestUrl));

        public static IObservable<HttpFetch<string>> Links(this IObservable<HttpFetch<ParsedHtml>> query) =>
            query.Links(null);

        public static IObservable<HttpFetch<T>> Links<T>(this IObservable<HttpFetch<ParsedHtml>> query, Func<string, HtmlObject, T> selector) =>
            Links(query, null, selector);

        public static IObservable<HttpFetch<string>> Links(this IObservable<HttpFetch<ParsedHtml>> query, Uri baseUrl) =>
            Links(query, baseUrl, (href, _) => href);

        public static IObservable<HttpFetch<T>> Links<T>(this IObservable<HttpFetch<ParsedHtml>> query, Uri baseUrl, Func<string, HtmlObject, T> selector) =>
            from html in query
            from link in html.Content.Links((href, ho) => html.WithContent(selector(href, ho)))
            select link;

        public static IObservable<HtmlObject> Tables(this IObservable<ParsedHtml> query) =>
            from html in query
            from t in html.Tables()
            select t;

        public static IObservable<HttpFetch<HtmlObject>> Tables(this IObservable<HttpFetch<ParsedHtml>> query) =>
            query.LiftTranslate(Tables);

        public static IObservable<DataTable> FormsAsDataTable(this IObservable<ParsedHtml> query) =>
            from html in query
            from forms in Observable.Return(html.FormsAsDataTable())
            select forms;

        public static IObservable<HttpFetch<DataTable>> FormsAsDataTable(this IObservable<HttpFetch<ParsedHtml>> query) =>
            query.LiftTranslate(FormsAsDataTable);

        public static IObservable<HtmlForm> Forms(this IObservable<ParsedHtml> query) =>
            from html in query
            from forms in html.Forms.ToObservable()
            select forms;

        public static IObservable<HttpFetch<HtmlForm>> Forms(this IObservable<HttpFetch<ParsedHtml>> query) =>
            query.LiftTranslate(Forms);

        static IObservable<HttpFetch<TOutput>> LiftTranslate<TInput, TOutput>(this IObservable<HttpFetch<TInput>> query, Func<IObservable<TInput>, IObservable<TOutput>> converter) =>
            from input in query
            from output in converter(Observable.Return(input.Content))
            select input.WithContent(output);
    }
}
