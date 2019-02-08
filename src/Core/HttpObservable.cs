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
    using WebLinq;

    public interface IHttpObservable : IObservable<HttpFetch<Unit>>
    {
        HttpOptions Options { get; }
        IHttpObservable WithOptions(HttpOptions options);
        Func<HttpConfig, HttpConfig> Configurer { get; }
        Func<HttpFetchInfo, bool> FilterPredicate { get; }
        IHttpObservable WithConfigurer(Func<HttpConfig, HttpConfig> modifier);
        IHttpObservable WithFilterPredicate(Func<HttpFetchInfo, bool> predicate);
        IObservable<HttpFetch<T>> ReadContent<T>(Func<HttpFetch<HttpContent>, Task<T>> reader);
    }

    public static partial class HttpObservable
    {
        public static IHttpObservable ReturnErroneousFetch(this IHttpObservable query) =>
            query.WithOptions(query.Options.WithReturnErroneousFetch(true));

        [Obsolete("Use " + nameof(ReturnErroneousFetch) + " instead.")]
        public static IHttpObservable ReturnErrorneousFetch(this IHttpObservable query) =>
            query.WithOptions(query.Options.WithReturnErroneousFetch(true));

        public static IHttpObservable SkipErroneousFetch(this IHttpObservable query) =>
            query.ReturnErroneousFetch()
                 .Filter(f => f.IsSuccessStatusCode);

        public static IHttpObservable SetHeader(this IHttpObservable query, string name, string value) =>
            query.WithConfigurer(c => query.Configurer(c).WithHeader(name, value));

        public static IObservable<HttpFetch<HttpContent>> Buffer(this IHttpObservable query) =>
            query.ReadContent(async f =>
            {
                await f.Content.LoadIntoBufferAsync().DontContinueOnCapturedContext();
                return f.Content;
            });

        public static IHttpObservable Do(this IHttpObservable query, Action<HttpFetchInfo> action) =>
            query.Filter(f => { action(f); return true; });

        public static IHttpObservable Filter(this IHttpObservable query, Func<HttpFetchInfo, bool> predicate) =>
            query.WithFilterPredicate(f => query.FilterPredicate(f) && predicate(f));

        internal static IHttpObservable Return(Func<IHttpObservable, IObservable<HttpFetch<HttpContent>>> query) =>
            Return(HttpOptions.Default, _ => _, query, _ => true);

        static IHttpObservable
            Return(HttpOptions options,
                   Func<HttpConfig, HttpConfig> configurer,
                   Func<IHttpObservable, IObservable<HttpFetch<HttpContent>>> query,
                   Func<HttpFetchInfo, bool> predicate) =>
            new Impl(query, options, configurer, predicate);

        internal static IHttpObservable Return(IObservable<IHttpObservable> query) =>
            Return(ho =>
                from q in query
                from e in q.WithConfigurer(ho.Configurer)
                           .WithOptions(ho.Options)
                           .WithFilterPredicate(ho.FilterPredicate)
                           .ReadContent(f => Task.FromResult(f.Content))
                select e);

        [Obsolete("Use " + nameof(IHttpObservable.ReadContent) + " instead.")]
        public static IObservable<HttpFetch<T>> WithReader<T>(this IHttpObservable query, Func<HttpFetch<HttpContent>, Task<T>> reader) =>
            query.ReadContent(reader);

        sealed class Impl : IHttpObservable
        {
            readonly Func<IHttpObservable, IObservable<HttpFetch<HttpContent>>> _query;

            public Impl(Func<IHttpObservable, IObservable<HttpFetch<HttpContent>>> query,
                        HttpOptions options,
                        Func<HttpConfig, HttpConfig> configurer,
                        Func<HttpFetchInfo, bool> predicate)
            {
                _query = query;
                Options = options;
                Configurer = configurer;
                FilterPredicate = predicate;
            }

            public IDisposable Subscribe(IObserver<HttpFetch<Unit>> observer) =>
                _query(this)
                    .Do(f => f.Content.Dispose())
                    .Select(f => f.WithContent(new Unit()))
                    .Where(f => FilterPredicate(f.Info))
                    .Subscribe(observer);

            public HttpOptions Options { get; }

            public IHttpObservable WithOptions(HttpOptions options) =>
                new Impl(_query, options, Configurer, FilterPredicate);

            public Func<HttpConfig, HttpConfig> Configurer { get; }

            public IHttpObservable WithConfigurer(Func<HttpConfig, HttpConfig> configurer) =>
                new Impl(_query, Options, configurer, FilterPredicate);

            public Func<HttpFetchInfo, bool> FilterPredicate { get; }

            public IHttpObservable WithFilterPredicate(Func<HttpFetchInfo, bool> predicate) =>
                new Impl(_query, Options, Configurer, predicate);

            public IObservable<HttpFetch<T>> ReadContent<T>(Func<HttpFetch<HttpContent>, Task<T>> reader) =>
                from f in _query(this)
                where FilterPredicate(f.Info)
                from c in reader(f)
                select f.WithContent(c);
        }
    }
}
