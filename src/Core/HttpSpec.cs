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
    using System.Net;
    using System.Net.Http;
    using Mannex.Collections.Generic;

    public sealed class HttpSpec
    {
        HttpOptions _options = new HttpOptions();
        HttpRequestMessage _request = new HttpRequestMessage();

        public HttpOptions Options => _options ?? (_options = new HttpOptions());
        public HttpRequestMessage Request => _request ?? (_request = new HttpRequestMessage());

        public HttpSpec ReturnErrorneousFetch()
        {
            Options.ReturnErrorneousFetch = true;
            return this;
        }

        public HttpSpec UserAgent(string value)
        {
            Request.Headers.UserAgent.ParseAdd(value);
            return this;
        }

        public HttpSpec Header(string name, string value)
        {
            Request.Headers.Add(name, value);
            return this;
        }

        HttpFetch<HttpContent> Send(HttpService http, HttpQueryState state, HttpMethod method, Uri url, HttpContent content = null)
        {
            var request = Request; _request = null;
            var options = _options; _options = null;
            request.Method = method;
            request.RequestUri = url;
            request.Content = content;
            return http.Send(request, state, options).ToHttpFetch(0);
        }

        public Query<HttpFetch<HttpContent>> Get(Uri url) =>
            from e in GetServices((svc, st) => new { Service = svc, State = st })
            select Send(e.Service, e.State, HttpMethod.Get, url);

        public Query<HttpFetch<HttpContent>> Post(Uri url, NameValueCollection data) =>
            Post(url, new FormUrlEncodedContent(from i in Enumerable.Range(0, data.Count)
                                                from v in data.GetValues(i)
                                                select data.GetKey(i).AsKeyTo(v)));

        public Query<HttpFetch<HttpContent>> Post(Uri url, HttpContent content) =>
            from e in GetServices((svc, st) => new { Service = svc, State = st })
            select Send(e.Service, e.State, HttpMethod.Post, url, content);

        static Query<T> GetServices<T>(Func<HttpService, HttpQueryState, T> selector) =>
            from currentState in Query.FindService<HttpQueryState>()
            from state in currentState != null
                          ? Query.Singleton(currentState)
                          : from defaultState in Query.Singleton(HttpQueryState.Default.WithCookies(new CookieContainer()))
                            from _ in Query.SetService(defaultState).Ignore()
                            select defaultState
            from service in Query.GetService<HttpService>()
            select selector(service, state);
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