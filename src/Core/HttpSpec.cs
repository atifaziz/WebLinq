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
    using System.Collections.Specialized;
    using System.Linq;
    using System.Net.Http;
    using Mannex.Collections.Generic;

    public sealed class HttpSpec
    {
        bool _returnErrorneousFetch;

        HttpHeaderCollection Headers { get; set; }

        public HttpSpec() { Headers = HttpHeaderCollection.Empty; }

        public HttpSpec ReturnErrorneousFetch()
        {
            _returnErrorneousFetch = true;
            return this;
        }

        public HttpSpec UserAgent(string value) { return Header("User-Agent", value); }

        public HttpSpec Header(string name, string value)
        {
            Headers = Headers.Set(name, value);
            return this;
        }

        public Query<HttpFetch<HttpContent>> Get(Uri url) =>
            from ua in Query.TryGetItem("Http.User-Agent", (bool found, string value) => found ? value : null)
            from context in Query.GetContext()
            select context.Eval((HttpService http) => http.Get(url, Options(ua)));

        public Query<HttpFetch<HttpContent>> Post(Uri url, NameValueCollection data) =>
            Post(url, new FormUrlEncodedContent(from i in Enumerable.Range(0, data.Count)
                                                from v in data.GetValues(i)
                                                select data.GetKey(i).AsKeyTo(v)));

        public Query<HttpFetch<HttpContent>> Post(Uri url, HttpContent content) =>
            from ua in Query.TryGetItem("Http.User-Agent", (bool found, string value) => found ? value : null)
            from context in Query.GetContext()
            select context.Eval((HttpService http) => http.Post(url, content, Options(ua)));

        HttpOptions Options(string ua)
        {
            var headers = HttpHeaderCollection.Empty;
            if (!string.IsNullOrEmpty(ua) && (Headers.IsEmpty || Headers.TryGetValue("User-Agent") == null))
                headers = headers.Set("User-Agent", ua);

            return new HttpOptions
            {
                ReturnErrorneousFetch = _returnErrorneousFetch,
                Headers = headers,
            };
        }
    }

    static class SysNetHttpExtensions
    {
        public static HttpFetch<HttpContent> ToHttpFetch(this HttpResponseMessage response, int id)
        {
            if (response == null) throw new ArgumentNullException(nameof(response));
            var request = response.RequestMessage;
            return HttpFetch.Create(id, response.Content,
                                    response.Version,
                                    response.StatusCode, response.ReasonPhrase,
                                    HttpHeaderCollection.Empty.Set(response.Headers),
                                    request.RequestUri,
                                    HttpHeaderCollection.Empty.Set(request.Headers));
        }
    }
}