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
    using System.Collections.Specialized;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Mime;
    using System.Xml.Linq;
    using Mannex.Collections.Specialized;
    using Mannex.Data;
    using Mannex.IO;
    using Mannex.Web;

    public static class Query
    {
        public static HttpSpec Http => new HttpSpec();

        public static Query<T> Create<T>(Func<QueryContext, QueryResult<T>> func) =>
            new Query<T>(func);

        public static Query<T> Return<T>(T value) =>
            Create(context => QueryResult.Create(context, value));

        public static Query<string> Text(this Query<HttpResponseMessage> query) =>
            query.Bind(response => Create(context => QueryResult.Create(context, response.Content.ReadAsStringAsync().Result)));

        public static Query<ParsedHtml> Html(string html) =>
            Html(html, null);

        public static Query<ParsedHtml> Html(string html, Uri baseUrl) =>
            Create(context => Html(context, html, baseUrl, p => p));

        public static Query<ParsedHtml> Html(this Query<HttpResponseMessage> query) =>
            Html(query, (_, html) => html);

        public static Query<T> Html<T>(this Query<HttpResponseMessage> query, Func<int, ParsedHtml, T> selector) =>
            query.Bind(response => Create(context =>
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

        public static Query<XDocument> XDocument(this Query<HttpResponseMessage> query) =>
            XDocument(query, LoadOptions.None);

        public static Query<XDocument> XDocument(this Query<HttpResponseMessage> query, LoadOptions options) =>
            query.Bind(response => new Query<XDocument>(context => QueryResult.Create(context, System.Xml.Linq.XDocument.Load(response.Content.ReadAsStreamAsync().Result, options))));

        public static SeqQuery<T> Spread<T>(this Query<IEnumerable<T>> query) =>
            SeqQuery.Create(query.Invoke);

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

        public static IEnumerable<T> ToEnumerable<T>(this Query<IEnumerable<T>> query, QueryContext context)
        {
            var result = query.Invoke(context);
            return result.DataOrDefault() ?? Enumerable.Empty<T>();
        }

        public static IEnumerable<T> ToEnumerable<T>(this SeqQuery<T> query, Func<QueryContext> contextFactory)
        {
            var result = query.Invoke(contextFactory());
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var e in result.DataOrDefault() ?? Enumerable.Empty<T>())
                yield return e;
        }

        public static Query<HttpResponseMessage> Submit(this Query<HttpResponseMessage> query, string formSelector, NameValueCollection data) =>
            query.Html().Bind(html => Submit(html, formSelector, data));

        public static Query<HttpResponseMessage> Submit(ParsedHtml html, string formSelector, NameValueCollection data) =>
            Create(context => context.Eval((IWebClient wc) =>
            {
                var forms = html.GetForms(formSelector, (fe, id, name, fa, fm, enctype) => fe.GetForm(fd => new
                {
                    Action  = new Uri(html.TryBaseHref(fa), UriKind.Absolute),
                    Method  = fm,
                    EncType = enctype, // TODO validate
                    Data    = fd,
                }));

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

                var submissionResponse =
                    form.Method == HtmlFormMethod.Post
                    ? wc.Post(form.Action, form.Data, null)
                    : wc.Get(new UriBuilder(form.Action) { Query = form.Data.ToW3FormEncoded() }.Uri, null);

                return QueryResult.Create(context, submissionResponse);
            }));

        public static Query<Zip> Unzip(this Query<HttpResponseMessage> query) =>
            query.Bind(response =>
            {
                var content = response.Content;
                var actualMediaType = content.Headers.ContentType.MediaType;
                const string zipMediaType = MediaTypeNames.Application.Zip;
                if (actualMediaType != null && !zipMediaType.Equals(actualMediaType, StringComparison.OrdinalIgnoreCase))
                    throw new Exception($"Expected content of type \"{zipMediaType}\" but received \"{actualMediaType}\" instead.");

                return Create(context =>
                {
                    var path = Path.GetTempFileName();
                    using (var output = File.Create(path))
                        content.CopyToAsync(output).Wait();
                    return QueryResult.Create(context, new Zip(path));
                });
            });

        public static Query<DataTable> XsvToDataTable(string text, string delimiter, bool quoted, params DataColumn[] columns) =>
            Create(context =>
                QueryResult.Create(context, text.Read().ParseXsvAsDataTable(delimiter, quoted, columns)));
    }
}
