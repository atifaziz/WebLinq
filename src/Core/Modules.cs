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
    using Html;

    public static class HttpModule
    {
        public static IHttpQuery<HttpFetch<ParsedHtml>> Html(this IHttpQuery query) =>
            query.Html(HtmlParser.Default);

        public static IHttpQuery Submit(this IHttpQuery query, string formSelector, NameValueCollection data) =>
            Submit(query, formSelector, null, null, data);

        public static IHttpQuery Submit(this IHttpQuery query, int formIndex, NameValueCollection data) =>
            Submit(query, null, formIndex, null, data);

        public static IHttpQuery SubmitTo(this IHttpQuery query, Uri url, string formSelector, NameValueCollection data) =>
            Submit(query, formSelector, null, url, data);

        public static IHttpQuery SubmitTo(this IHttpQuery query, Uri url, int formIndex, NameValueCollection data) =>
            Submit(query, null, formIndex, url, data);

        static IHttpQuery Submit(IHttpQuery query, string? formSelector, int? formIndex, Uri? url,
                                 NameValueCollection data) =>
            HttpQuery.Flatten(from html in query.Html()
                              select HttpQuery.Submit(html.Content, formSelector, formIndex, url, data));

        public static IHttpQuery Submit(this IHttpQuery query, string formSelector, ISubmissionData<Unit> data) =>
            Submit(query, formSelector, null, null, data);

        public static IHttpQuery Submit(this IHttpQuery query, int formIndex, ISubmissionData<Unit> data) =>
            Submit(query, null, formIndex, null, data);

        public static IHttpQuery SubmitTo(this IHttpQuery query, Uri url, string formSelector, ISubmissionData<Unit> data) =>
            Submit(query, formSelector, null, url, data);

        public static IHttpQuery SubmitTo(this IHttpQuery query, Uri url, int formIndex, ISubmissionData<Unit> data) =>
            Submit(query, null, formIndex, url, data);

        static IHttpQuery Submit(IHttpQuery query, string? formSelector, int? formIndex, Uri? url, ISubmissionData<Unit> data) =>
            query.Html().Submit(formSelector, formIndex, url, data);

        public static IHttpQuery<HttpFetch<string>> Links(this IHttpQuery query) =>
            Links(query, (href, _) => href);

        public static IHttpQuery<HttpFetch<T>> Links<T>(this IHttpQuery query, Func<string, HtmlObject, T> selector) =>
            query.Html().Links(selector);

        public static IHttpQuery<HttpFetch<HtmlObject>> Tables(this IHttpQuery query) =>
            query.Html().Tables();

        public static IHttpQuery<HttpFetch<DataTable>> FormsAsDataTable(this IHttpQuery query) =>
            query.Html().FormsAsDataTable();

        public static IHttpQuery<HttpFetch<HtmlForm>> Forms(this IHttpQuery query) =>
            query.Html().Forms();
    }

    public static class HtmlModule
    {
        public static IHtmlParser HtmlParser => Html.HtmlParser.Default;

        public static ParsedHtml ParseHtml(string html) =>
            ParseHtml(html, null);

        public static ParsedHtml ParseHtml(string html, Uri? baseUrl) =>
            HtmlParser.Parse(html, baseUrl);

        public static IEnumerable<string> Links(string html) =>
            Links(html, null, (href, _) => href);

        public static IEnumerable<T> Links<T>(string html, Func<string, HtmlObject, T> selector) =>
            Links(html, null, selector);

        public static IEnumerable<string> Links(string html, Uri baseUrl) =>
            Links(html, baseUrl, (href, _) => href);

        public static IEnumerable<T> Links<T>(string html, Uri? baseUrl, Func<string, HtmlObject, T> selector) =>
            ParseHtml(html, baseUrl).Links(selector);

        public static IEnumerable<HtmlObject> Tables(string html) =>
            ParseHtml(html).Tables();
    }
}
