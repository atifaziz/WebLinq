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
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Mime;

    public static class HtmlQuery
    {
        public static Query<ParsedHtml> Html(string html) =>
            Html(html, null);

        public static Query<ParsedHtml> Html(string html, Uri baseUrl) =>
            Query.Create(context => Html(context, html, baseUrl, p => p));

        public static Query<HttpFetch<ParsedHtml>> Html(this Query<HttpFetch<HttpContent>> query) =>
            query.Accept(MediaTypeNames.Text.Html)
                 .Bind(fetch => Query.Create(context => Html(context, fetch.Content.ReadAsStringAsync().Result, fetch.RequestUrl, fetch.WithContent)));

        static QueryResult<T> Html<T>(QueryContext context, string html, Uri baseUrl, Func<ParsedHtml, T> selector) =>
            QueryResult.Create(context, context.Eval((IHtmlParser hps) => selector(hps.Parse(html, baseUrl))));

        public static SeqQuery<string> Links(string html) =>
            Links(html, null, (href, _) => href);

        public static SeqQuery<T> Links<T>(string html, Func<string, string, T> selector) =>
            Links(html, null, selector);

        public static SeqQuery<string> Links(string html, Uri baseUrl) =>
            Links(html, baseUrl, (href, _) => href);

        public static SeqQuery<T> Links<T>(string html, Uri baseUrl, Func<string, string, T> selector) =>
            Html(html, baseUrl).Bind(ph => Links(ph, selector));

        public static SeqQuery<string> Links(ParsedHtml html) =>
            Links(html, (href, _) => href);

        public static SeqQuery<T> Links<T>(ParsedHtml html, Func<string, string, T> selector) =>
            SeqQuery.Create(context => Links(context, html, selector));

        public static SeqQuery<HttpFetch<T>> Links<T>(this Query<HttpFetch<HttpContent>> query, Func<string, string, T> selector) =>
            Links(query, null, selector);

        public static SeqQuery<HttpFetch<T>> Links<T>(this Query<HttpFetch<HttpContent>> query, Uri baseUrl, Func<string, string, T> selector) =>
            query.Html().Bind(html => SeqQuery.Create(context => Links(context, html.Content, (href, txt) => html.WithContent(selector(href, txt)))));

        static QueryResult<IEnumerable<T>> Links<T>(QueryContext context, ParsedHtml html, Func<string, string, T> selector) =>
            QueryResult.Create(context, html.Links((href, ho) => selector(href, ho.InnerHtml)));

        public static SeqQuery<string> Tables(string html) =>
            Html(html).Bind(Tables);

        public static SeqQuery<string> Tables(ParsedHtml html) =>
            SeqQuery.Create(context => Tables(context, html));

        public static SeqQuery<string> Tables(this Query<HttpFetch<HttpContent>> query) =>
            query.Html().Bind(html => SeqQuery.Create(context => Tables(context, html.Content)));

        static QueryResult<IEnumerable<string>> Tables(QueryContext context, ParsedHtml html) =>
            QueryResult.Create(context, html.Tables(null));
    }
}
