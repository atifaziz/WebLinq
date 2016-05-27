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

        public static Query<ParsedHtml> Html(this Query<HttpResponseMessage> query) =>
            Html(query, (_, html) => html);

        public static Query<T> Html<T>(this Query<HttpResponseMessage> query, Func<int, ParsedHtml, T> selector) =>
            query.Bind(response => Query.Create(context =>
            {
                var content = response.Content;

                const string htmlMediaType = MediaTypeNames.Text.Html;
                var actualMediaType = content.Headers.ContentType.MediaType;
                if (!htmlMediaType.Equals(actualMediaType, StringComparison.OrdinalIgnoreCase))
                    throw new Exception($"Expected content of type \"{htmlMediaType}\" but received \"{actualMediaType}\" instead.");

                return Html(context, content.ReadAsStringAsync().Result, response.RequestMessage.RequestUri, html => selector(HttpId.Get(response), html));
            }));

        static QueryResult<T> Html<T>(QueryContext context, string html, Uri baseUrl, Func<ParsedHtml, T> selector) =>
            QueryResult.Create(context, context.Eval((IHtmlParser hps) => selector(hps.Parse(html, baseUrl))));
/*
        public static Query<ParsedHtml> Html(string html) =>
            Html(html, null);

        public static Query<ParsedHtml> Html(string html, Uri baseUrl) =>
            Query.Create(context => Html(context, html, baseUrl));

        public static Query<ParsedHtml> Html(this Query<HttpResponseMessage> query) =>
            query.Bind(response => Query.Create(context =>
            {
                var content = response.Content;

                const string htmlMediaType = MediaTypeNames.Text.Html;
                var actualMediaType = content.Headers.ContentType.MediaType;
                if (!htmlMediaType.Equals(actualMediaType, StringComparison.OrdinalIgnoreCase))
                    throw new Exception($"Expected content of type \"{htmlMediaType}\" but received \"{actualMediaType}\" instead.");

                return Html(context, content.ReadAsStringAsync().Result, response.RequestMessage.RequestUri);
            }));

        static QueryResult<ParsedHtml> Html(QueryContext context, string html, Uri baseUrl) =>
            QueryResult.Create(context, context.Eval((IHtmlParser hps) => hps.Parse(html, baseUrl)));
*/
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

        public static SeqQuery<T> Links<T>(this Query<HttpResponseMessage> query, Func<string, string, T> selector) =>
            Links(query, null, selector);

        public static SeqQuery<T> Links<T>(this Query<HttpResponseMessage> query, Uri baseUrl, Func<string, string, T> selector) =>
            query.Html().Bind(html => SeqQuery.Create(context => Links(context, html, selector)));

        static QueryResult<IEnumerable<T>> Links<T>(QueryContext context, ParsedHtml html, Func<string, string, T> selector) =>
            QueryResult.Create(context, html.Links((href, ho) => selector(href, ho.InnerHtml)));

        public static SeqQuery<string> Tables(string html) =>
            Html(html).Bind(Tables);

        public static SeqQuery<string> Tables(ParsedHtml html) =>
            SeqQuery.Create(context => Tables(context, html));

        public static SeqQuery<string> Tables(this Query<HttpResponseMessage> query) =>
            query.Html().Bind(html => SeqQuery.Create(context => Tables(context, html)));

        static QueryResult<IEnumerable<string>> Tables(QueryContext context, ParsedHtml html) =>
            QueryResult.Create(context, html.Tables(null));
    }
}
