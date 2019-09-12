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
        IContentObservable<T>
            ExpandContent<TSeed, TState, T>(Func<HttpFetch<HttpContent>, Task<TSeed>> seeder,
                                            Func<TSeed, TState> initializer,
                                            Func<TState, Task<(TState State, bool Continue, T Item)>> looper,
                                            Action<TSeed> disposer);
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

        public static IHttpObservable SetUserAgent(this IHttpObservable query, string value) =>
            query.WithConfigurer(c => query.Configurer(c).WithUserAgent(value));

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

        public static IContentObservable<T>
            ExpandContent<TResource, T>(this IHttpObservable query,
                Func<HttpFetch<HttpContent>, Task<TResource>> seeder,
                Func<TResource, Task<(bool, T)>> looper)
                where TResource : IDisposable =>
            query.ExpandContent(seeder, r => r, async r => { var (some, item) = await looper(r); return (r, some, item); });

        public static IContentObservable<T>
            ExpandContent<TSeed, TState, T>(this IHttpObservable query,
                Func<HttpFetch<HttpContent>, Task<TSeed>> seeder,
                Func<TSeed, TState> initializer,
                Func<TState, Task<(TState, bool, T)>> looper)
                where TSeed : IDisposable =>
            query.ExpandContent(seeder, initializer, looper, r => r.Dispose());

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

            public IContentObservable<T>
                ExpandContent<TSeed, TState, T>(
                    Func<HttpFetch<HttpContent>, Task<TSeed>> seeder,
                    Func<TSeed, TState> initializer,
                    Func<TState, Task<(TState State, bool Continue, T Item)>> looper,
                    Action<TSeed> disposer) =>
                ContentObservable.Create<T>((options, observer) =>
                    _query(this)
                        .SelectMany(seeder)
                        .Select(seed =>
                            Observable
                                .Return((State   : initializer(seed),
                                         Count   : 0,
                                         Previous: default(T),
                                         Continue: true,
                                         Current : default(T)))
                                .Expand(s => from e in options.IterationPredicate(s.Count, s.Previous)
                                                     ? Observable.Return(s.State)
                                                                 .SelectMany(looper)
                                                                 .TakeWhile(e => e.Continue
                                                                              && options.ContinuationPredicate(e.Item, s.Count))
                                                     : Observable.Empty<(TState State, bool Continue, T Item)>()
                                             select (e.State, s.Count + 1, e.Item, e.Continue, e.Item))
                                .Skip(1)
                                .Finally(() => disposer(seed)))
                        .Concat()
                        .Select(e => e.Current)
                        .Subscribe(observer));
        }
    }

    public sealed class ContentOptions<T>
    {
        public static readonly ContentOptions<T> Default =
            new ContentOptions<T>(delegate { return true; },
                                  delegate { return true; });

        public Func<int, T, bool> IterationPredicate { get; private set; }
        public Func<T, int, bool> ContinuationPredicate { get; private set; }

        ContentOptions(Func<int, T, bool> iterationPredicate,
                       Func<T, int, bool> continuationPredicate)
        {
            IterationPredicate = iterationPredicate;
            ContinuationPredicate = continuationPredicate;
        }

        ContentOptions(ContentOptions<T> other) :
            this(other.IterationPredicate, other.ContinuationPredicate)
        { }

        public ContentOptions<T> WithIterationPredicate(Func<int, T, bool> value) =>
            IterationPredicate == value ? this : new ContentOptions<T>(this) { IterationPredicate = value };

        public ContentOptions<T> AndIterationPredicate(Func<int, T, bool> predicate) =>
            WithIterationPredicate((i, pe) => IterationPredicate(i, pe) && predicate(i, pe));

        public ContentOptions<T> WithContinuationPredicate(Func<T, int, bool> value) =>
            ContinuationPredicate == value ? this : new ContentOptions<T>(this) { ContinuationPredicate = value };

        public ContentOptions<T> AndContinuationPredicate(Func<T, int, bool> predicate) =>
            WithContinuationPredicate((e, i) => ContinuationPredicate(e, i) && predicate(e, i));
    }

    public interface IContentObservable<T> : IObservable<T>
    {
        ContentOptions<T> Options { get; }
        IContentObservable<T> WithOptions(ContentOptions<T> value);
    }

    public sealed class ContentObservable<T> : IContentObservable<T>
    {
        readonly Func<ContentOptions<T>, IObserver<T>, IDisposable> _subscriber;

        public ContentObservable(Func<ContentOptions<T>, IObserver<T>, IDisposable> subscriber) :
            this(ContentOptions<T>.Default, subscriber) {}

        ContentObservable(ContentOptions<T> options, Func<ContentOptions<T>, IObserver<T>, IDisposable> subscriber)
        {
            Options     = options ?? throw new ArgumentNullException(nameof(options));
            _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
        }

        public IDisposable Subscribe(IObserver<T> observer) =>
            _subscriber(Options, observer);

        public ContentOptions<T> Options { get; }

        public IContentObservable<T> WithOptions(ContentOptions<T> value) =>
            value == Options ? this : new ContentObservable<T>(value, _subscriber);
    }

    public static class ContentObservable
    {
        public static IContentObservable<T> Create<T>(Func<ContentOptions<T>, IObserver<T>, IDisposable> subscriber) =>
            new ContentObservable<T>(subscriber);

        public static IContentObservable<T> TakeWhile<T>(this IContentObservable<T> source, Func<T, int, bool> predicate) =>
            source.WithOptions(source.Options.AndContinuationPredicate(predicate));

        public static IContentObservable<T> Take<T>(this IContentObservable<T> source, int count) =>
            source.WithOptions(source.Options.AndIterationPredicate((i, _) => i < count));

        public static IContentObservable<T> TakeUntil<T>(this IContentObservable<T> source, Func<T, bool> predicate) =>
            source.WithOptions(source.Options.AndIterationPredicate((i, pe) => i == 0 || !predicate(pe)));
    }
}
