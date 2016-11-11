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
    using System.Linq;
    using System.Net.Http;
    using System.Net.Mime;
    using Mannex.Collections.Generic;
    using Mannex.Collections.Specialized;

    public static class HtmlQuery
    {
        public static IQuery<ParsedHtml> Html(string html) =>
            Html(html, null);

        public static IQuery<ParsedHtml> Html(string html, Uri baseUrl) =>
            from hps in Query.GetService<IHtmlParser>()
            select hps.Parse(html, baseUrl);

        public static IQuery<HttpFetch<ParsedHtml>> Html(this IQuery<HttpFetch<HttpContent>> query) =>
            from fetch in query.Accept(MediaTypeNames.Text.Html)
            from hps in Query.GetService<IHtmlParser>()
            select fetch.WithContent(hps.Parse(fetch.Content.ReadAsStringAsync().Result, fetch.RequestUrl));

        public static IQuery<string> Links(string html) =>
            Links(html, null, (href, _) => href);

        public static IQuery<T> Links<T>(string html, Func<string, string, T> selector) =>
            Links(html, null, selector);

        public static IQuery<string> Links(string html, Uri baseUrl) =>
            Links(html, baseUrl, (href, _) => href);

        public static IQuery<T> Links<T>(string html, Uri baseUrl, Func<string, string, T> selector) =>
            from ph in Html(html, baseUrl)
            from link in Links(ph, selector)
            select link;

        public static IQuery<string> Links(ParsedHtml html) =>
            Links(html, (href, _) => href);

        public static IQuery<T> Links<T>(ParsedHtml html, Func<string, string, T> selector) =>
            html.Links((href, ho) => selector(href, ho.InnerHtml))
                .Select(link => link)
                .ToQuery();

        public static IQuery<HttpFetch<string>> Links(this IQuery<HttpFetch<HttpContent>> query) =>
            query.Links(null);

        public static IQuery<HttpFetch<T>> Links<T>(this IQuery<HttpFetch<HttpContent>> query, Func<string, string, T> selector) =>
            Links(query, null, selector);

        public static IQuery<HttpFetch<string>> Links(this IQuery<HttpFetch<HttpContent>> query, Uri baseUrl) =>
            Links(query, baseUrl, (href, _) => href);

        public static IQuery<HttpFetch<T>> Links<T>(this IQuery<HttpFetch<HttpContent>> query, Uri baseUrl, Func<string, string, T> selector) =>
            from html in query.Html()
            from link in Links(html.Content, (href, txt) => html.WithContent(selector(href, txt)))
            select link;

        public static IQuery<HtmlObject> Tables(string html) =>
            from ph in Html(html)
            from t in Tables(html)
            select t;

        public static IQuery<HtmlObject> Tables(ParsedHtml html) =>
            html.Tables(null).ToQuery();

        public static IQuery<HttpFetch<HtmlObject>> Tables(this IQuery<HttpFetch<HttpContent>> query) =>
            from f in query.Html()
            from t in Tables(f.Content)
            select f.WithContent(t);

        public static IQuery<HttpFetch<DataTable>> FormsAsDataTable(this IQuery<HttpFetch<HttpContent>> query) =>
            query.Html().FormsAsDataTable();

        public static IQuery<DataTable> FormsAsDataTable(this IQuery<ParsedHtml> query) =>
            from html in query
            from forms in FormsAsDataTable(html)
            select forms;

        public static IQuery<HttpFetch<DataTable>> FormsAsDataTable(this IQuery<HttpFetch<ParsedHtml>> query) =>
            from html in query
            from forms in FormsAsDataTable(html.Content)
            select html.WithContent(forms);

        public static IQuery<DataTable> FormsAsDataTable(ParsedHtml html)
        {
            var forms =
                from f in html.Forms
                select f.GetForm((fd, fs) => new
                {
                    f.Name,
                    Action       = new Uri(html.TryBaseHref(f.Action), UriKind.Absolute),
                    Method       = f.Method.ToString().ToUpperInvariant(),
                    f.EncType,
                    Data         = fd,
                    Submittables = fs,
                });

            var dt = new DataTable();
            dt.Columns.AddRange(new []
            {
                new DataColumn("#")                         { AllowDBNull = false },
                new DataColumn("FormName")                  { AllowDBNull = true  },
                new DataColumn("FormAction")                { AllowDBNull = false },
                new DataColumn("FormMethod")                { AllowDBNull = true  },
                new DataColumn("FormEncoding")              { AllowDBNull = true  },
                new DataColumn("Name")                      { AllowDBNull = false },
                new DataColumn("Value")                     { AllowDBNull = false },
                new DataColumn("Submittable", typeof(bool)) { AllowDBNull = false },
            });

            foreach (var form in
                from fi in forms.Select((f, i) => (i + 1).AsKeyTo(f))
                let form = fi.Value
                from controls in new[]
                {
                    from e in form.Data.AsEnumerable()
                    from v in e.Value
                    select new { e.Key, Value = v, Submittable = false },
                    from e in form.Submittables.AsEnumerable()
                    from v in e.Value
                    select new { e.Key, Value = v, Submittable = true  },
                }
                from control in controls
                select new object[]
                {
                    fi.Key,
                    form.Name,
                    form.Action.OriginalString,
                    form.Method,
                    form.EncType,
                    control.Key,
                    control.Value,
                    control.Submittable,
                })
            {
                dt.Rows.Add(form);
            }

            return Query.Singleton(dt);
        }

        public static IQuery<HttpFetch<HtmlForm>> Forms(this IQuery<HttpFetch<HttpContent>> query) =>
            query.Html().Forms();

        public static IQuery<HtmlForm> Forms(this IQuery<ParsedHtml> query) =>
            from html in query
            from forms in Forms(html)
            select forms;

        public static IQuery<HttpFetch<HtmlForm>> Forms(this IQuery<HttpFetch<ParsedHtml>> query) =>
            from html in query
            from forms in Forms(html.Content)
            select html.WithContent(forms);

        public static IQuery<HtmlForm> Forms(ParsedHtml html) =>
            Query.Return(html.Forms);
    }
}
