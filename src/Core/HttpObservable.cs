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
        Func<HttpConfig, HttpConfig> Configurer { get; }
        IHttpObservable WithOptions(HttpOptions options);
        IHttpObservable WithHttpConfigurer(Func<HttpConfig, HttpConfig> configurer);
        IObservable<HttpFetch<T>> WithReader<T>(Func<HttpFetch<HttpContent>, Task<T>> reader);
    }

    public static class HttpObservable
    {
        public static IHttpObservable AppendHttpConfigurer(this IHttpObservable query, Func<HttpConfig, HttpConfig> f) =>
            query.WithHttpConfigurer(config => f(query.Configurer(config)));

        public static IHttpClient<HttpConfig> WithTimeout(this IHttpClient<HttpConfig> client, TimeSpan duration) =>
            client.WithConfig(client.Config.WithTimeout(duration));

        public static IHttpObservable WithRequestTimeout(this IHttpObservable query, TimeSpan duration) =>
            query.AppendHttpConfigurer(config => config.WithTimeout(duration));

        public static IHttpObservable WithRequestUserAgent(this IHttpObservable query, string ua) =>
            query.AppendHttpConfigurer(config => config.WithUserAgent(ua));

        public static IHttpObservable ReturnErrorneousFetch(this IHttpObservable query) =>
            query.WithOptions(query.Options.WithReturnErrorneousFetch(true));

        public static IObservable<HttpFetch<HttpContent>> Buffer(this IHttpObservable query) =>
            query.WithReader(async f =>
            {
                await f.Content.LoadIntoBufferAsync().DontContinueOnCapturedContext();
                return f.Content;
            });

        public static IHttpObservable Do(this IHttpObservable query, Action<HttpFetch<HttpContent>> action) =>
            Return(query.Options, options => query.WithOptions(options).WithReader(f => { action(f); return Task.FromResult(f.Content); }));

        internal static IHttpObservable Return(Func<HttpOptions, IObservable<HttpFetch<HttpContent>>> query) =>
            Return(HttpOptions.Default, query);

        internal static IHttpObservable Return(HttpOptions options, Func<HttpOptions, IObservable<HttpFetch<HttpContent>>> query) =>
            new Impl(query, options);

        internal static IHttpObservable Return(IObservable<IHttpObservable> query) =>
            Return(options =>
                from q in query
                from e in q.WithOptions(options).WithReader(f => Task.FromResult(f.Content))
                select e);

        sealed class Impl : IHttpObservable
        {
            readonly Func<HttpOptions, IObservable<HttpFetch<HttpContent>>> _query;

            public Impl(Func<HttpOptions, IObservable<HttpFetch<HttpContent>>> query, HttpOptions options, Func<HttpConfig, HttpConfig> configurer = null)
            {
                _query = query;
                Options = options;
                Configurer = configurer ?? (_ => _);
            }

            public IDisposable Subscribe(IObserver<HttpFetch<Unit>> observer) =>
                _query(Options)
                    .Do(f => f.Content.Dispose())
                    .Select(f => f.WithContent(new Unit()))
                    .Subscribe(observer);

            public HttpOptions Options { get; }

            public IHttpObservable WithOptions(HttpOptions options) =>
                new Impl(_query, options, Configurer);

            public IObservable<HttpFetch<T>> WithReader<T>(Func<HttpFetch<HttpContent>, Task<T>> reader) =>
                from f in _query(Options)
                from c in reader(f)
                select f.WithContent(c);

            public Func<HttpConfig, HttpConfig> Configurer { get; }

            public IHttpObservable WithHttpConfigurer(Func<HttpConfig, HttpConfig> value) =>
                Configurer == value ? this : new Impl(_query, Options, config => Configurer(config));
        }
    }
}