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

    public interface IHttpObservable : IObservable<HttpFetch<Unit>>
    {
        IObservable<HttpFetch<T>> WithReader<T>(Func<HttpFetch<HttpContent>, Task<T>> reader);
    }

    public static class HttpObservable
    {
        public static IObservable<HttpFetch<HttpContent>> Buffer(this IHttpObservable query) =>
            query.WithReader(async f =>
            {
                await f.Content.LoadIntoBufferAsync().DontContinueOnCapturedContext();
                return f.Content;
            });

        public static IHttpObservable Do(this IHttpObservable query, Action<HttpFetch<HttpContent>> action) =>
            Return(query.WithReader(f => {action(f); return Task.FromResult(f.Content); }));

        internal static IHttpObservable Return(IObservable<HttpFetch<HttpContent>> query) =>
            new Impl(query);

        public static IHttpObservable Return(IObservable<IHttpObservable> query) =>
            Return(from q in query
                   from e in q.WithReader(f => Task.FromResult(f.Content))
                   select e);

        sealed class Impl : IHttpObservable
        {
            readonly IObservable<HttpFetch<HttpContent>> _query;

            public Impl(IObservable<HttpFetch<HttpContent>> query)
            {
                _query = query;
            }

            public IDisposable Subscribe(IObserver<HttpFetch<Unit>> observer) =>
                _query.Do(f => f.Content.Dispose())
                      .Select(f => f.WithContent(new Unit()))
                      .Subscribe(observer);

            public IObservable<HttpFetch<T>> WithReader<T>(Func<HttpFetch<HttpContent>, Task<T>> reader) =>
                from f in _query
                from c in reader(f)
                select f.WithContent(c);
        }
    }
}