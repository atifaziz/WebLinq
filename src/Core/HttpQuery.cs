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
    using System.Net.Http;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    public class HttpQuery : IObservable<HttpFetch<Unit>>
    {
        readonly Func<HttpOptions, IObservable<HttpFetch<HttpContent>>> _query;

        HttpQuery(Func<HttpOptions, IObservable<HttpFetch<HttpContent>>> query, HttpOptions options)
        {
            _query = query;
            Options = options;
        }

        public IDisposable Subscribe(IObserver<HttpFetch<Unit>> observer) =>
            _query(Options)
                .Do(f => f.Content.Dispose())
                .Select(f => f.WithContent(new Unit()))
                .Subscribe(observer);

        public HttpOptions Options { get; }

        public HttpQuery WithOptions(HttpOptions options) =>
            new HttpQuery(_query, options);

        public IObservable<HttpFetch<T>> WithReader<T>(Func<HttpFetch<HttpContent>, Task<T>> reader) =>
            from f in _query(Options)
            from c in reader(f)
            select f.WithContent(c);


        internal static HttpQuery Return(Func<HttpOptions, IObservable<HttpFetch<HttpContent>>> query) =>
            Return(HttpOptions.Default, query);

        internal static HttpQuery Return(HttpOptions options, Func<HttpOptions, IObservable<HttpFetch<HttpContent>>> query) =>
            new HttpQuery(query, options);

        internal static HttpQuery Return(IObservable<HttpQuery> query) =>
            Return(options =>
                from q in query
                from e in q.WithOptions(options).WithReader(f => Task.FromResult(f.Content))
                select e);
    }

    public static class HttpQueryExtensions
    {
        public static HttpQuery ReturnErrorneousFetch(this HttpQuery query) =>
            query.WithOptions(query.Options.WithReturnErrorneousFetch(true));

        public static IObservable<HttpFetch<HttpContent>> Buffer(this HttpQuery query) =>
            query.WithReader(async f =>
            {
                await f.Content.LoadIntoBufferAsync().DontContinueOnCapturedContext();
                return f.Content;
            });

        public static HttpQuery Do(this HttpQuery query, Action<HttpFetch<HttpContent>> action) =>
            HttpQuery.Return(query.Options, options => query.WithOptions(options).WithReader(f => { action(f); return Task.FromResult(f.Content); }));
    }
}