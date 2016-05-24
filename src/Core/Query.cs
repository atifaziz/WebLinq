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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Mime;

    public static class Query
    {
        public static HttpSpec Http => new HttpSpec();

        public static Query<T> Return<T>(T value) =>
            new Query<T>(context => new QueryResult<T>(context, value));

        public static Query<ParsedHtml> Html(string html, Uri baseUrl) =>
            Html(new StringContent(html), baseUrl);

        public static Query<ParsedHtml> Html(HttpResponseMessage response) =>
            Html(response.Content, response.RequestMessage.RequestUri);

        public static Query<ParsedHtml> Html(HttpContent content, Uri baseUrl) =>
            new Query<ParsedHtml>(context =>
            {
                const string htmlMediaType = MediaTypeNames.Text.Html;
                var actualMediaType = content.Headers.ContentType.MediaType;
                if (!htmlMediaType.Equals(actualMediaType, StringComparison.OrdinalIgnoreCase))
                    throw new Exception($"Expected content of type \"{htmlMediaType}\" but received \"{actualMediaType}\" instead.");

                return QueryResult.Create(context, context.Eval((IHtmlParser hps) => hps.Parse(content.ReadAsStringAsync().Result, baseUrl)));
            });

        public static SeqQuery<T> Spread<T>(this Query<IEnumerable<T>> query) =>
            new SeqQuery<T>(query.Invoke);

        public static SeqQuery<T> Links<T>(string html, Uri baseUrl, Func<string, string, T> selector) =>
            Links(new StringContent(html), null, selector);

        public static SeqQuery<T> Links<T>(HttpResponseMessage response, Func<string, string, T> selector) =>
            Links(response.Content, response.RequestMessage.RequestUri, selector);

        public static SeqQuery<T> Links<T>(HttpContent content, Uri baseUrl, Func<string, string, T> selector) =>
            Html(content, baseUrl).Bind(html => new SeqQuery<T>(context => QueryResult.Create(context, html.Links((href, ho) => selector(href, ho.InnerHtml)))));

        public static SeqQuery<string> Tables(string html) =>
            Tables(new StringContent(html));

        public static SeqQuery<string> Tables(HttpResponseMessage response) =>
            Tables(response.Content);

        public static SeqQuery<string> Tables(HttpContent content) =>
            Html(content, null).Bind(html => new SeqQuery<string>(context => QueryResult.Create(context, html.Tables(null))));

        public static IEnumerable<T> ToEnumerable<T>(this Query<IEnumerable<T>> query, QueryContext context)
        {
            var result = query.Invoke(context);
            return result.DataOrDefault() ?? Enumerable.Empty<T>();
        }

        public static IEnumerable<T> ToEnumerable<T>(this SeqQuery<T> query, QueryContext context)
        {
            var result = query.Invoke(context);
            return result.DataOrDefault() ?? Enumerable.Empty<T>();
        }
    }
}
