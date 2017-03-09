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
    using System.Net.Http;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    public interface IHttpObservable : IObservable<HttpFetch<Unit>>
    {
        HttpOptions Options { get; }
        IHttpObservable WithOptions(HttpOptions options);
        Func<HttpConfig, HttpConfig> Configurer { get; }
        IHttpObservable WithConfigurer(Func<HttpConfig, HttpConfig> modifier);
        IObservable<HttpFetch<T>> WithReader<T>(Func<HttpFetch<HttpContent>, Task<T>> reader);
    }

    public static class HttpObservable
    {
        public static IHttpObservable ReturnErrorneousFetch(this IHttpObservable query) =>
            query.WithOptions(query.Options.WithReturnErrorneousFetch(true));

        public static IHttpObservable SetHeader(this IHttpObservable query, string name, string value) =>
            query.WithConfigurer(c => query.Configurer(c).WithHeader(name, value));

        public static IObservable<HttpFetch<HttpContent>> Buffer(this IHttpObservable query) =>
            query.WithReader(async f =>
            {
                await f.Content.LoadIntoBufferAsync().DontContinueOnCapturedContext();
                return f.Content;
            });

        public static IHttpObservable Do(this IHttpObservable query, Action<HttpFetch<HttpContent>> action) =>
            Return(query.Options, query.Configurer,
                   ho => query.WithConfigurer(ho.Configurer)
                              .WithOptions(ho.Options)
                              .WithReader(f => { action(f); return Task.FromResult(f.Content); }));

        internal static IHttpObservable Return(Func<IHttpObservable, IObservable<HttpFetch<HttpContent>>> query) =>
            Return(HttpOptions.Default, _ => _, query);

        static IHttpObservable Return(HttpOptions options, Func<HttpConfig, HttpConfig> configurer, Func<IHttpObservable, IObservable<HttpFetch<HttpContent>>> query) =>
            new Impl(query, options, configurer);

        internal static IHttpObservable Return(IObservable<IHttpObservable> query) =>
            Return(ho =>
                from q in query
                from e in q.WithConfigurer(ho.Configurer)
                           .WithOptions(ho.Options)
                           .WithReader(f => Task.FromResult(f.Content))
                select e);

        sealed class Impl : IHttpObservable
        {
            readonly Func<IHttpObservable, IObservable<HttpFetch<HttpContent>>> _query;

            public Impl(Func<IHttpObservable, IObservable<HttpFetch<HttpContent>>> query, HttpOptions options, Func<HttpConfig, HttpConfig> configurer)
            {
                _query = query;
                Options = options;
                Configurer = configurer;
            }

            public IDisposable Subscribe(IObserver<HttpFetch<Unit>> observer) =>
                _query(this)
                    .Do(f => f.Content.Dispose())
                    .Select(f => f.WithContent(new Unit()))
                    .Subscribe(observer);

            public HttpOptions Options { get; }

            public IHttpObservable WithOptions(HttpOptions options) =>
                new Impl(_query, options, Configurer);

            public Func<HttpConfig, HttpConfig> Configurer { get; }

            public IHttpObservable WithConfigurer(Func<HttpConfig, HttpConfig> configurer) =>
                new Impl(_query, Options, configurer);

            public IObservable<HttpFetch<T>> WithReader<T>(Func<HttpFetch<HttpContent>, Task<T>> reader) =>
                from f in _query(this)
                from c in reader(f)
                select f.WithContent(c);
        }
    }
}