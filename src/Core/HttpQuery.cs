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

        public static Query<T> Content<T>(this Query<HttpFetch<T>> query) =>
            from e in query select e.Content;

        public static SeqQuery<T> Content<T>(this SeqQuery<HttpFetch<T>> query) =>
            from e in query select e.Content;

        public static Query<HttpFetch<HttpContent>> Accept(this Query<HttpFetch<HttpContent>> query, params string[] mediaTypes) =>
            query.Do(e =>
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

                if (mediaTypes.Any(mediaType => mediaType.Equals(actualMediaType, StringComparison.OrdinalIgnoreCase)))
                    return;

                throw new Exception($"Unexpected content of type \"{actualMediaType}\". Acceptable types are: {string.Join(", ", mediaTypes)}");
            });

        public static Query<HttpFetch<HttpContent>> Submit(this Query<HttpFetch<HttpContent>> query, string formSelector, NameValueCollection data) =>
            query.Html().Bind(html => Submit(html.Content, formSelector, data));

        public static Query<HttpFetch<HttpContent>> Submit(ParsedHtml html, string formSelector, NameValueCollection data)
        {
            var forms =
                from f in html.GetForms(formSelector)
                select new
                {
                    Action = new Uri(html.TryBaseHref(f.Action), UriKind.Absolute),
                    f.Method,
                    f.EncType, // TODO validate
                    Data = f.GetForm(),
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
