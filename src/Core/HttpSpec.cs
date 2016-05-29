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
    using System.Net.Http;

    public sealed class HttpSpec
    {
        NameValueCollection _headers;

        bool HasHeaders => _headers?.Count > 0;

        NameValueCollection Headers
        {
            get { return _headers ?? (_headers = new NameValueCollection()); }
            set { _headers = value; }
        }


        public HttpSpec UserAgent(string value) { return Header("User-Agent", value); }

        public HttpSpec Header(string name, string value)
        {
            Headers.Add(name, value);
            return this;
        }

        public Query<HttpFetch<HttpContent>> Get(Uri url) =>
            Get(url, (_, s) => s);

        public Query<T> Get<T>(Uri url, Func<int, HttpFetch<HttpContent>, T> selector) =>
            Query.Create(context => QueryResult.Create(new QueryContext(id: context.Id + 1,
                                                                        serviceProvider: context.ServiceProvider),
                                                                        context.Eval((HttpService http) => selector(context.Id, http.Get(url, new HttpOptions { Headers = new HttpHeaderCollection(Headers), FetchId = context.Id })))));

        public Query<HttpFetch<HttpContent>> Post(Uri url, NameValueCollection data) =>
            Post(url, data, (_, s) => s);

        public Query<T> Post<T>(Uri url, NameValueCollection data, Func<int, HttpFetch<HttpContent>, T> selector) =>
            Query.Create(context => QueryResult.Create(new QueryContext(id: context.Id + 1,
                                                                        serviceProvider: context.ServiceProvider),
                                                                        context.Eval((HttpService http) => selector(context.Id, http.Post(url, data, new HttpOptions { Headers = new HttpHeaderCollection(Headers), FetchId = context.Id })))));
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
                                    new HttpHeaderCollection(response.Headers),
                                    request.RequestUri,
                                    new HttpHeaderCollection(request.Headers));
        }
    }
}