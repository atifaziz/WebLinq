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
        HttpOptions Options { get; }
        IHttpObservable WithOptions(HttpOptions options);
        Func<HttpConfig, HttpConfig> Configurer { get; }
        Func<HttpFetch, bool> Predicate { get; }
        IHttpObservable WithConfigurer(Func<HttpConfig, HttpConfig> modifier);
        IHttpObservable WithPredicate(Func<HttpFetch, bool> predicate);
        IObservable<HttpFetch<T>> WithReader<T>(Func<HttpFetch, HttpContent, Task<T>> reader);
    }

    public static partial class HttpObservable
    {
        public static IHttpObservable ReturnErroneousFetch(this IHttpObservable query) =>
            query.WithOptions(query.Options.WithReturnErroneousFetch(true));

        [Obsolete("Use " + nameof(ReturnErroneousFetch) + " instead.")]
        public static IHttpObservable ReturnErrorneousFetch(this IHttpObservable query) =>
            query.WithOptions(query.Options.WithReturnErroneousFetch(true));

        public static IHttpObservable SetHeader(this IHttpObservable query, string name, string value) =>
            query.WithConfigurer(c => query.Configurer(c).WithHeader(name, value));

        public static IObservable<HttpFetch<HttpContent>> Buffer(this IHttpObservable query) =>
            query.WithReader(async (_, content) =>
            {
                await content.LoadIntoBufferAsync().DontContinueOnCapturedContext();
                return content;
            });

        public static IHttpObservable Do(this IHttpObservable query, Action<HttpFetch> action) =>
            query.Where(f => { action(f); return true; });

        public static IHttpObservable Where(this IHttpObservable query, Func<HttpFetch, bool> predicate) =>
            query.WithPredicate(f => query.Predicate(f) && predicate(f));

        internal static IHttpObservable Return(Func<IHttpObservable, IObservable<HttpFetch<HttpContent>>> query) =>
            Return(HttpOptions.Default, _ => _, query, _ => true);

        static IHttpObservable
            Return(HttpOptions options,
                   Func<HttpConfig, HttpConfig> configurer,
                   Func<IHttpObservable, IObservable<HttpFetch<HttpContent>>> query,
                   Func<HttpFetch, bool> predicate) =>
            new Impl(query, options, configurer, predicate);

        internal static IHttpObservable Return(IObservable<IHttpObservable> query) =>
            Return(ho =>
                from q in query
                from e in q.WithConfigurer(ho.Configurer)
                           .WithOptions(ho.Options)
                           .WithPredicate(ho.Predicate)
                           .WithReader((_, c) => Task.FromResult(c))
                select e);

        sealed class Impl : IHttpObservable
        {
            readonly Func<IHttpObservable, IObservable<HttpFetch<HttpContent>>> _query;

            public Impl(Func<IHttpObservable, IObservable<HttpFetch<HttpContent>>> query,
                        HttpOptions options,
                        Func<HttpConfig, HttpConfig> configurer,
                        Func<HttpFetch, bool> predicate)
            {
                _query = query;
                Options = options;
                Configurer = configurer;
                Predicate = predicate;
            }

            public IDisposable Subscribe(IObserver<HttpFetch<Unit>> observer) =>
                _query(this)
                    .Do(f => f.Content.Dispose())
                    .Select(f => f.WithContent(new Unit()))
                    .Where(f => Predicate(f))
                    .Subscribe(observer);

            public HttpOptions Options { get; }

            public IHttpObservable WithOptions(HttpOptions options) =>
                new Impl(_query, options, Configurer, Predicate);

            public Func<HttpConfig, HttpConfig> Configurer { get; }

            public IHttpObservable WithConfigurer(Func<HttpConfig, HttpConfig> configurer) =>
                new Impl(_query, Options, configurer, Predicate);

            public Func<HttpFetch, bool> Predicate { get; }

            public IHttpObservable WithPredicate(Func<HttpFetch, bool> predicate) =>
                new Impl(_query, Options, Configurer, predicate);

            public IObservable<HttpFetch<T>> WithReader<T>(Func<HttpFetch, HttpContent, Task<T>> reader) =>
                from f in _query(this)
                where Predicate(f)
                from c in reader(f, f.Content)
                select f.WithContent(c);
        }
    }
}
