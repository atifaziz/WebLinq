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

    public static class HttpRequestBuilder
    {
        public static HttpRequestBuilder<T> Then<T>(this Query<HttpFetch<T>> query) =>
            new HttpRequestBuilder<T>(query);
    }

    public sealed class HttpRequestBuilder<T>
    {
        readonly Query<HttpFetch<T>> _query;
        HttpOptions _options = new HttpOptions();
        HttpRequestMessage _request = new HttpRequestMessage();

        public HttpOptions Options => _options ?? (_options = new HttpOptions());
        public HttpRequestMessage Request => _request ?? (_request = new HttpRequestMessage());

        public HttpRequestBuilder() :
            this(Query.Singleton(default(HttpFetch<T>))) { }

        internal HttpRequestBuilder(Query<HttpFetch<T>> query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            _query = query;
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

        HttpFetch<HttpContent> Send(IHttpService http, HttpQueryState state, TypedValue<HttpFetchId, int> id, HttpMethod method, Uri url, HttpContent content = null)
        {
            var request = Request; _request = null;
            var options = _options; _options = null;
            request.Method = method;
            request.RequestUri = url;
            request.Content = content;
            return http.Send(request, state, options).ToHttpFetch(id.Value);
        }

        public Query<HttpFetch<HttpContent>> Get(Uri url) =>
            from _ in _query.Ignore()
            from e in GetServices((svc, st, fid) => new { Service = svc, State = st, FetchId = fid })
            select Send(e.Service, e.State, e.FetchId, HttpMethod.Get, url);

        public Query<HttpFetch<HttpContent>> Post(Uri url, NameValueCollection data) =>
            Post(url, new FormUrlEncodedContent(from i in Enumerable.Range(0, data.Count)
                                                from v in data.GetValues(i)
                                                select data.GetKey(i).AsKeyTo(v)));

        public Query<HttpFetch<HttpContent>> Post(Uri url, HttpContent content) =>
            from _ in _query.Ignore()
            from e in GetServices((svc, st, fid) => new { Service = svc, State = st, FetchId = fid })
            select Send(e.Service, e.State, e.FetchId, HttpMethod.Post, url, content);

        static Query<T> GetServices<T>(Func<IHttpService, HttpQueryState, TypedValue<HttpFetchId, int>, T> selector) =>
            from context in Query.GetContext()
            from http in Query.FindService<IHttpService>()
            from state in Query.FindService<HttpQueryState>()
            from id in Query.FindService<Ref<TypedValue<HttpFetchId, int>>>()
            let hsp = new HttpServicesProvider
            (
                http ?? new HttpService(),
                state ?? HttpQueryState.Default.WithCookies(new CookieContainer()),
                (id ?? Ref.Create(HttpFetchId.New(0))).Updating(x => HttpFetchId.New(x + 1)),
                context
            )
            from _ in Query.SetContext(context.WithServiceProvider(hsp))
            select selector(hsp.Http, hsp.State, hsp.Id);

        sealed class HttpServicesProvider : IServiceProvider
        {
            readonly IServiceProvider _provider;

            public IHttpService Http { get; }
            public HttpQueryState State { get; }
            public Ref<TypedValue<HttpFetchId, int>> Id { get; }

            public HttpServicesProvider(IHttpService http, HttpQueryState state, Ref<TypedValue<HttpFetchId, int>> id, IServiceProvider provider)
            {
                Id = id;
                Http = http;
                State = state;
                _provider = provider;
            }

            public object GetService(Type serviceType) =>
                  serviceType == typeof(IHttpService) ? Http
                : serviceType == typeof(HttpQueryState) ? State
                : serviceType == typeof(Ref<TypedValue<HttpFetchId, int>>) ? Id
                : _provider?.GetService(serviceType);
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