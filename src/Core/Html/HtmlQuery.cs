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

    public static class HtmlQuery
    {
        public static IHttpQuery<HttpFetch<ParsedHtml>> Html(this IHttpQuery query, IHtmlParser parser) =>
            from e in query.Accept(MediaTypeNames.Text.Html)
                           .Text()
            select e.WithContent(parser.Parse(e.Content, e.RequestUrl));

        public static IHttpQuery<HttpFetch<string>> Links(this IHttpQuery<HttpFetch<ParsedHtml>> query) =>
            Links(query, (href, _) => href);

        public static IHttpQuery<HttpFetch<T>> Links<T>(this IHttpQuery<HttpFetch<ParsedHtml>> query,
                                                        Func<string, HtmlObject, T> selector) =>
            from html in query
            from link in html.Content.Links((href, ho) => html.WithContent(selector(href, ho)))
            select link;

        public static IHttpQuery<HtmlObject> Tables(this IHttpQuery<ParsedHtml> query) =>
            from html in query
            from t in html.Tables()
            select t;

        public static IHttpQuery<HttpFetch<HtmlObject>> Tables(this IHttpQuery<HttpFetch<ParsedHtml>> query) =>
            query.LiftTranslate(Tables);

        public static IHttpQuery<DataTable> FormsAsDataTable(this IHttpQuery<ParsedHtml> query) =>
            from html in query
            from forms in HttpQuery.Return(html.FormsAsDataTable())
            select forms;

        public static IHttpQuery<HttpFetch<DataTable>> FormsAsDataTable(this IHttpQuery<HttpFetch<ParsedHtml>> query) =>
            query.LiftTranslate(FormsAsDataTable);

        public static IHttpQuery<HtmlForm> Forms(this IHttpQuery<ParsedHtml> query) =>
            from html in query
            from forms in html.Forms
            select forms;

        public static IHttpQuery<HttpFetch<HtmlForm>> Forms(this IHttpQuery<HttpFetch<ParsedHtml>> query) =>
            query.LiftTranslate(Forms);

        static IHttpQuery<HttpFetch<TOutput>>
            LiftTranslate<TInput, TOutput>(this IHttpQuery<HttpFetch<TInput>> query,
                                           Func<IHttpQuery<TInput>, IHttpQuery<TOutput>> converter) =>
            from input in query
            from output in converter(HttpQuery.Return(input.Content))
            select input.WithContent(output);
    }
}
