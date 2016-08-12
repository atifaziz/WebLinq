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
    #region Imports

    using System;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using Html;
    using Mannex.Collections.Specialized;
    using Mannex.Web;
    using Mime;

    #endregion

    public static class HttpQuery
    {
        public static HttpSpec Http => new HttpSpec();

        public static Query<HttpClient> HttpClient(string defaultUserAgent)
        {
            var client = new HttpClient();
            if (!string.IsNullOrEmpty(defaultUserAgent))
                client.DefaultRequestHeaders.UserAgent.ParseAdd(defaultUserAgent);
            return UseHttpClient(client);
        }

        public static Query<HttpClient> UseHttpClient(HttpClient client) =>
            Query.SetService(client);

        public static Query<T> Content<T>(this Query<HttpFetch<T>> query) =>
            from e in query select e.Content;

        public static Query<HttpFetch<HttpContent>> Accept(this Query<HttpFetch<HttpContent>> query, params string[] mediaTypes) =>
            (mediaTypes?.Length ?? 0) == 0
            ? query
            : query.Do(e =>
            {
                var headers = e.Content.Headers;
                var actualMediaType = headers.ContentType?.MediaType;
                if (actualMediaType == null)
                {
                    var contentDisposition = headers.ContentDisposition;
                    var filename = contentDisposition?.FileName ?? contentDisposition?.FileNameStar;
                    if (!string.IsNullOrEmpty(filename))
                        actualMediaType = MimeMapping.FindMimeTypeFromFileName(filename);
                    if (actualMediaType == null)
                    {
                        throw new Exception($"Content has unspecified type when acceptable types are: {string.Join(", ", mediaTypes)}");
                    }
                }

                Debug.Assert(mediaTypes != null);
                if (mediaTypes.Any(mediaType => string.Equals(mediaType, actualMediaType, StringComparison.OrdinalIgnoreCase)))
                    return;

                throw new Exception($"Unexpected content of type \"{actualMediaType}\". Acceptable types are: {string.Join(", ", mediaTypes)}");
            });

        public static Query<HttpFetch<HttpContent>> Submit(this Query<HttpFetch<HttpContent>> query, string formSelector, NameValueCollection data) =>
            Submit(query, formSelector, null, data);

        public static Query<HttpFetch<HttpContent>> Submit(this Query<HttpFetch<HttpContent>> query, int formIndex, NameValueCollection data) =>
            Submit(query, null, formIndex, data);

        static Query<HttpFetch<HttpContent>> Submit(this Query<HttpFetch<HttpContent>> query, string formSelector, int? formIndex, NameValueCollection data) =>
            from html in query.Html()
            from fetch in Submit(html.Content, formSelector, formIndex, data)
            select fetch;

        public static Query<HttpFetch<HttpContent>> Submit(ParsedHtml html, string formSelector, NameValueCollection data) =>
            Submit(html, formSelector, null, data);

        public static Query<HttpFetch<HttpContent>> Submit(ParsedHtml html, int formIndex, NameValueCollection data) =>
            Submit(html, null, formIndex, data);

        static Query<HttpFetch<HttpContent>> Submit(ParsedHtml html,
                                                    string formSelector, int? formIndex,
                                                    NameValueCollection data)
        {
            var forms =
                from f in formIndex == null
                          ? html.QueryFormSelectorAll(formSelector)
                          : formIndex < html.Forms.Count
                          ? Enumerable.Repeat(html.Forms[(int) formIndex], 1)
                          : Enumerable.Empty<HtmlForm>()
                select new
                {
                    Action = new Uri(html.TryBaseHref(f.Action), UriKind.Absolute),
                    f.Method,
                    f.EncType, // TODO validate
                    Data = f.GetSubmissionData(),
                };

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

            return form.Method == HtmlFormMethod.Post
                 ? Http.Post(form.Action, form.Data)
                 : Http.Get(new UriBuilder(form.Action) { Query = form.Data.ToW3FormEncoded() }.Uri);
        }

        public static Query<HttpFetch<T>> ExceptStatusCode<T>(this Query<HttpFetch<T>> query, params HttpStatusCode[] statusCodes) =>
            query.Do(e =>
            {
                if (e.IsSuccessStatusCode || statusCodes.Any(sc => e.StatusCode == sc))
                    return;
                (e.Content as IDisposable)?.Dispose();
                throw new HttpRequestException($"Response status code does not indicate success: {e.StatusCode}.");
            });
    }
}
