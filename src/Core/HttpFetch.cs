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
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reactive;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using RxUnit = System.Reactive.Unit;
    using Choices;

    public interface IHttpContentReader<T>
    {
        Task<HttpFetch<T>> Read(HttpFetch<HttpContent> fetch);
    }

    public interface IHttpFetchReader<out T>
    {
        T Read(IHttpFetch fetch);
    }

    public static class HttpFetchReader
    {
        public static IHttpFetchReader<T> Create<T>(Func<IHttpFetch, T> f) =>
            new Reader<T>(f);

        public static IHttpFetchReader<T> Return<T>(T value) =>
            Create(_ => value);

        public static IHttpFetchReader<TResult>
            Bind<T, TResult>(this IHttpFetchReader<T> source, Func<T, IHttpFetchReader<TResult>> fun) =>
            Create(f => fun(source.Read(f)).Read(f));

        public static IHttpFetchReader<TResult>
            Select<T, TResult>(this IHttpFetchReader<T> source, Func<T, TResult> selector) =>
            source.Bind(x => Return(selector(x)));

        public static IHttpFetchReader<TResult>
            SelectMany<TFirst, TSecond, TResult>(this IHttpFetchReader<TFirst> first,
                Func<TFirst, IHttpFetchReader<TSecond>> secondSelector,
                Func<TFirst, TSecond, TResult> resultSelector) =>
            first.Bind(x => secondSelector(x).Bind(y => Return(resultSelector(x, y))));

        public static IHttpFetchReader<HttpHeaderCollection> ContentHeaders() =>
            Create(f => f.ContentHeaders);

        public static IHttpFetchReader<HttpStatusCode> StatusCode() =>
            Create(f => f.StatusCode);

        public static IHttpFetchReader<string> MediaType() =>
            from hs in ContentHeaders()
            select hs.TryGetValue("Content-Type", out var v) ? v : null into v
            select v.FirstOrDefault() is string s
                 ? new System.Net.Mime.ContentType(s).MediaType
                 : null;

        sealed class Reader<T> : IHttpFetchReader<T>
        {
            readonly Func<IHttpFetch, T> _f;
            public Reader(Func<IHttpFetch, T> f) => _f = f;
            public T Read(IHttpFetch fetch) => _f(fetch);
        }
    }

    static class X
    {
        public static Choice<T1, T2, T3>
            Choose<T1, T2, T3>(bool f1, Func<T1> c1,
                bool f2, Func<T2> c2,
                Func<T3> c3)
            => f1 ? Choice<T1, T2, T3>.Choice1(c1())
             : f2 ? Choice<T1, T2, T3>.Choice2(c2())
             : Choice<T1, T2, T3>.Choice3(c3());

        public static void Test1()
        {
            var q =
                from t in HttpFetchReader.MediaType()
                select "text/plain".Equals(t, StringComparison.OrdinalIgnoreCase)
                     ? Choice<IHttpContentReader<string>,
                              IHttpContentReader<XDocument>,
                              IHttpContentReader<Unit>>.Choice1(HttpContentReader.Text())
                     : t.StartsWith("application/xml", StringComparison.OrdinalIgnoreCase)
                     ? Choice<IHttpContentReader<string>,
                              IHttpContentReader<XDocument>,
                              IHttpContentReader<Unit>>.Choice2(HttpContentReader.Xml())
                     : Choice<IHttpContentReader<string>,
                              IHttpContentReader<XDocument>,
                              IHttpContentReader<Unit>>.Choice3(HttpContentReader.Unit());
        }

        public static void Test2()
        {
            var q =
                from t in HttpFetchReader.MediaType()
                select Choose("text/plain".Equals(t, StringComparison.OrdinalIgnoreCase),
                                  () => HttpContentReader.Text().Select(f => new { f.Content }),
                              t.StartsWith("application/xml", StringComparison.OrdinalIgnoreCase),
                                  HttpContentReader.Xml,
                              HttpContentReader.Unit);
        }

        public static void Test3()
        {
            var q =
                from t in HttpFetchReader.MediaType()
                select
                    Choice.If("text/plain".Equals(t, StringComparison.OrdinalIgnoreCase),
                        () => HttpContentReader.Text().Select(f => f.WithContent(new { f.Content })),
                        () =>
                            Choice.If(t.StartsWith("application/xml", StringComparison.OrdinalIgnoreCase),
                                HttpContentReader.Xml,
                                HttpContentReader.Unit));
        }

        public static void Test4()
        {
            var q =
                from sc in HttpFetchReader.StatusCode()
                from t in sc == HttpStatusCode.OK ? HttpFetchReader.MediaType() : HttpFetchReader.Return<string>(null)
                select
                    Choice.If(sc == HttpStatusCode.OK,
                        () => sc,
                        () =>
                            Choice.If("text/plain".Equals(t, StringComparison.OrdinalIgnoreCase),
                                () => HttpContentReader.Text().Select(f => f.WithContent(new { f.Content })),
                                () =>
                                    Choice.If(t.StartsWith("application/xml", StringComparison.OrdinalIgnoreCase),
                                        HttpContentReader.Xml,
                                        HttpContentReader.Unit)));
        }
    }

    public static class HttpContentReader
    {
        public static IHttpContentReader<TResult>
            Select<T, TResult>(this IHttpContentReader<T> reader,
                               Func<HttpFetch<T>, TResult> selector) =>
            Create(async f => f.WithContent(selector(await reader.Read(f).DontContinueOnCapturedContext())));

        public static IHttpContentReader<string> Text() => Text(_ => _);

        public static IHttpContentReader<T> Text<T>(Func<string, T> selector) =>
            Text().Select(f => selector(f.Content));

        public static IHttpContentReader<XDocument> Xml() => Xml(_ => _);

        public static IHttpContentReader<T> Xml<T>(Func<XDocument, T> selector) =>
            Create(async f => f.WithContent(selector(XDocument.Load(await f.Content.ReadAsStreamAsync().ConfigureAwait(false)))));

        public static IHttpContentReader<Unit> Unit() =>
            Create(f => { f.Content.Dispose(); return Task.FromResult(f.WithContent(RxUnit.Default)); });


        public static IHttpContentReader<T> Create<T>(Func<HttpFetch<HttpContent>, Task<HttpFetch<T>>> f) =>
            new Reader<T>(f);

        sealed class Reader<T> : IHttpContentReader<T>
        {
            readonly Func<HttpFetch<HttpContent>, Task<HttpFetch<T>>> _impl;

            public Reader(Func<HttpFetch<HttpContent>, Task<HttpFetch<T>>> impl) =>
                _impl = impl ?? throw new ArgumentNullException(nameof(impl));

            public Task<HttpFetch<T>> Read(HttpFetch<HttpContent> fetch) =>
                _impl(fetch ?? throw new ArgumentNullException(nameof(fetch)));
        }

        public static IHttpContentReader<T> Return<T>(T value) =>
            Create(f => Task.FromResult(f.WithContent(value)));
    }

    public static class HttpFetch
    {
        public static HttpFetch<T> Create<T>(int id,
                                             T content,
                                             IHttpClient client,
                                             Version httpVersion,
                                             HttpStatusCode statusCode,
                                             string reasonPhrase,
                                             HttpHeaderCollection headers,
                                             HttpHeaderCollection contentHeaders,
                                             Uri requestUrl,
                                             HttpHeaderCollection requestHeaders) =>
            new HttpFetch<T>(id, content, client,
                             httpVersion, statusCode, reasonPhrase, headers, contentHeaders,
                             requestUrl, requestHeaders);
    }

    public interface IHttpFetch
    {
        int Id                              { get; }
        IHttpClient Client                  { get; }
        Version HttpVersion                 { get; }
        HttpStatusCode StatusCode           { get; }
        string ReasonPhrase                 { get; }
        HttpHeaderCollection Headers        { get; }
        HttpHeaderCollection ContentHeaders { get; }
        Uri RequestUrl                      { get; }
        HttpHeaderCollection RequestHeaders { get; }
    }

    [DebuggerDisplay("Id = {Id}, StatusCode = {StatusCode} ({ReasonPhrase}), Content = {Content}")]
    public partial class HttpFetch<T> : IHttpFetch, IDisposable
    {
        bool _disposed;

        public int Id                              { get; }
        public T Content                           { get; private set; }
        public IHttpClient Client                  { get; }
        public Version HttpVersion                 { get; }
        public HttpStatusCode StatusCode           { get; }
        public string ReasonPhrase                 { get; }
        public HttpHeaderCollection Headers        { get; }
        public HttpHeaderCollection ContentHeaders { get; }
        public Uri RequestUrl                      { get; }
        public HttpHeaderCollection RequestHeaders { get; }

        public HttpFetch(int id,
                         T content,
                         IHttpClient client,
                         Version httpVersion,
                         HttpStatusCode statusCode,
                         string reasonPhrase,
                         HttpHeaderCollection headers,
                         HttpHeaderCollection contentHeaders,
                         Uri requestUrl,
                         HttpHeaderCollection requestHeaders)
        {
            Id             = id;
            Content        = content;
            Client         = client;
            HttpVersion    = httpVersion;
            StatusCode     = statusCode;
            ReasonPhrase   = reasonPhrase;
            Headers        = headers;
            ContentHeaders = contentHeaders;
            RequestUrl     = requestUrl;
            RequestHeaders = requestHeaders;
        }

        public bool IsSuccessStatusCode => IsSuccessStatusCodeInRange(200, 299);
        public bool IsSuccessStatusCodeInRange(int first, int last) => (int)StatusCode >= first && (int)StatusCode <= last;

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            var disposable = Content as IDisposable;
            Content = default;
            disposable?.Dispose();
        }

        public HttpFetch<TContent> WithContent<TContent>(TContent content) =>
            new HttpFetch<TContent>(Id, content, Client,
                                    HttpVersion, StatusCode, ReasonPhrase, Headers, ContentHeaders,
                                    RequestUrl, RequestHeaders);

        public HttpFetch<T> WithConfig(HttpConfig config) =>
            new HttpFetch<T>(Id, Content, Client.WithConfig(config),
                             HttpVersion, StatusCode, ReasonPhrase, Headers, ContentHeaders,
                             RequestUrl, RequestHeaders);
    }
}
