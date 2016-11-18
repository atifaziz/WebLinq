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
    using System.Linq;
    using System.Net.Http;
    using Mannex.Collections.Generic;

    public static class HttpRequestBuilder
    {
        public static HttpRequestBuilder<HttpFetch<T>> Then<T>(this IEnumerable<HttpFetch<T>> query) =>
            new HttpRequestBuilder<HttpFetch<T>>(query);

        public static HttpRequestBuilder<HttpConfig> Then(this IEnumerable<HttpConfig> query) =>
            new HttpRequestBuilder<HttpConfig>(query);
    }

    public sealed class HttpRequestBuilder<T>
    {
        HttpConfig _config;
        readonly IEnumerable<T> _query;
        HttpOptions _options = new HttpOptions();
        HttpRequestMessage _request = new HttpRequestMessage();

        public HttpOptions Options => _options ?? (_options = new HttpOptions());
        public HttpRequestMessage Request => _request ?? (_request = new HttpRequestMessage());

        public HttpRequestBuilder() :
            this(new T[1]) { }

        internal HttpRequestBuilder(IEnumerable<T> query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            _query = query;
        }

        public HttpRequestBuilder(HttpConfig config) : this()
        {
            _config = config;
        }

        public HttpRequestBuilder<T> ReturnErrorneousFetch()
        {
            Options.ReturnErrorneousFetch = true;
            return this;
        }

        public HttpRequestBuilder<T> UserAgent(string value)
        {
            Request.Headers.UserAgent.ParseAdd(value);
            return this;
        }

        public HttpRequestBuilder<T> Header(string name, string value)
        {
            Request.Headers.Add(name, value);
            return this;
        }

        HttpFetch<HttpContent> Send(IHttpClient http, HttpConfig config, int id, HttpMethod method, Uri url, HttpContent content = null)
        {
            var request = Request; _request = null;
            var options = _options; _options = null;
            request.Method = method;
            request.RequestUri = url;
            request.Content = content;
            return http.Send(request, config ?? _config ?? HttpConfig.Default, options).ToHttpFetch(id);
        }

        public IEnumerable<HttpFetch<HttpContent>> Get(Uri url) => Get(null, url);

        public IEnumerable<HttpFetch<HttpContent>> Get(HttpConfig config, Uri url) =>
            from _ in _query
            from http in HttpClient.Default
            select Send(http, config, 0, HttpMethod.Get, url);

        public IEnumerable<HttpFetch<HttpContent>> Post(Uri url, NameValueCollection data) =>
            Post(null, url, data);

        public IEnumerable<HttpFetch<HttpContent>> Post(HttpConfig config, Uri url, NameValueCollection data) =>
            Post(config, url, new FormUrlEncodedContent(from i in Enumerable.Range(0, data.Count)
                                                from v in data.GetValues(i)
                                                select data.GetKey(i).AsKeyTo(v)));

        public IEnumerable<HttpFetch<HttpContent>> Post(Uri url, HttpContent content) =>
            Post(null, url, content);

        public IEnumerable<HttpFetch<HttpContent>> Post(HttpConfig config, Uri url, HttpContent content) =>
            from _ in _query
            from http in HttpClient.Default
            select Send(http, config, 0, HttpMethod.Post, url, content);
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