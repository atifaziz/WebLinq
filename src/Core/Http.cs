#region Copyright (c) 2022 Atif Aziz. All rights reserved.
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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WebLinq.Html;
using WebLinq.Mime;

namespace WebLinq;

public delegate HttpConfig HttpConfigModifier(HttpConfig config);

#pragma warning disable CA1724 // Type names should not match namespaces (ergonomic)
public static class Http
#pragma warning restore CA1724 // Type names should not match namespaces
{
    public static IHttpQuery<T> Apply<T>(this HttpConfigModifier modifier, IHttpQuery<T> query)
    {
        if (modifier == null) throw new ArgumentNullException(nameof(modifier));
        if (query == null) throw new ArgumentNullException(nameof(query));

        return WebLinq.HttpQuery.Create((context, cancellationToken) =>
        {
            context.Config = modifier(context.Config);
            return query.GetAsyncEnumerator(context, cancellationToken);
        });
    }

    public static HttpConfigModifier Config(HttpConfig config) => _ => config;

    public static HttpConfigModifier Config(params HttpConfigModifier[] modifiers)
    {
        if (modifiers == null) throw new ArgumentNullException(nameof(modifiers));

        return modifiers.DefaultIfEmpty(static config => config)
                        .Aggregate((a, m) => config => m(a(config)));
    }

    public static HttpConfigModifier SetHeader(string name, string value)
    {
        if (name == null) throw new ArgumentNullException(nameof(name));
        if (value == null) throw new ArgumentNullException(nameof(value));

        return config => config.WithHeader(name, value);
    }

    public static HttpConfigModifier SetHeader(this HttpConfigModifier modifier, string name, string value)
    {
        if (modifier == null) throw new ArgumentNullException(nameof(modifier));
        if (name == null) throw new ArgumentNullException(nameof(name));
        if (value == null) throw new ArgumentNullException(nameof(value));

        return config => modifier(config).WithHeader(name, value);
    }

    public static HttpConfigModifier Proxy(Uri? url) => config => config.WithProxyUrl(url);

    public static HttpConfigModifier Proxy(this HttpConfigModifier modifier, Uri? url)
    {
        if (modifier == null) throw new ArgumentNullException(nameof(modifier));

        return modifier + Proxy(url);
    }

    public static IHttpQuery Get(this HttpConfigModifier modifier, Uri url) =>
        modifier.Apply(Get(url));

    public static IHttpQuery Post(this HttpConfigModifier modifier, Uri url, NameValueCollection data) =>
        modifier.Apply(Post(url, data));

    public static IHttpQuery Apply(this HttpConfigModifier modifier, IHttpQuery query) => new HttpConfigModifierQuery(modifier, query);

    sealed class HttpConfigModifierQuery : IHttpQuery
    {
        readonly HttpConfigModifier _modifier;
        readonly IHttpQuery _query;

        public HttpConfigModifierQuery(HttpConfigModifier modifier, IHttpQuery query)
        {
            _modifier = modifier;
            _query = query;
        }

        public IAsyncEnumerator<HttpFetchInfo> GetAsyncEnumerator(HttpQueryContext context, CancellationToken cancellationToken)
        {
            context.Config = _modifier(context.Config);
            return _query.GetAsyncEnumerator(context, cancellationToken);
        }

        public HttpQuerySetup Setup => _query.Setup;

        public IHttpQuery WithSetup(HttpQuerySetup value) => new HttpConfigModifierQuery(_modifier, _query.WithSetup(value));

        public IHttpQuery<HttpFetch<TContent>> ReadContent<TContent>(IHttpContentReader<TContent> reader) =>
            new Query<HttpFetch<TContent>>(_modifier, _query.ReadContent(reader));

        sealed class Query<T> : IHttpQuery<T>
        {
            readonly HttpConfigModifier _modifier;
            readonly IHttpQuery<T> _query;

            public Query(HttpConfigModifier modifier, IHttpQuery<T> query)
            {
                _modifier = modifier;
                _query = query;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(HttpQueryContext context, CancellationToken cancellationToken)
            {
                context.Config = _modifier(context.Config);
                return _query.GetAsyncEnumerator(context, cancellationToken);
            }
        }
    }

    public static IHttpQuery Get(Uri url)
    {
        if (url == null) throw new ArgumentNullException(nameof(url));

        return new HttpQuery(delegate
        {
            return Task.FromResult(new HttpRequestMessage(HttpMethod.Get, url));
        });
    }

    public static IHttpQuery Get(this IHttpQuery query, Uri url)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        if (url == null) throw new ArgumentNullException(nameof(url));

        return new HttpQuery(query, delegate
        {
            return Task.FromResult(new HttpRequestMessage(HttpMethod.Get, url));
        });
    }

    public static IHttpQuery Post(Uri url, NameValueCollection data)
    {
        if (url == null) throw new ArgumentNullException(nameof(url));
        if (data == null) throw new ArgumentNullException(nameof(data));

        return new HttpQuery((_, _) => Task.FromResult(CreatePostRequest(url, data)));
    }

    public static IHttpQuery PostJson(Uri url, object? data) =>
        Post(url, JsonSerializer.Serialize(data), System.Net.Mime.MediaTypeNames.Application.Json);

    public static IHttpQuery Post(Uri url, string data, string? mediaType = null)
    {
        if (url == null) throw new ArgumentNullException(nameof(url));
        if (data == null) throw new ArgumentNullException(nameof(data));

        return new HttpQuery(delegate
        {
            return Task.FromResult(new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(data, null, mediaType)
            });
        });
    }

    public static IHttpQuery Post(this IHttpQuery query, Uri url, NameValueCollection data)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        if (url == null) throw new ArgumentNullException(nameof(url));
        if (data == null) throw new ArgumentNullException(nameof(data));

        return new HttpQuery(query, delegate
        {
            return Task.FromResult(CreatePostRequest(url, data));
        });
    }

    static HttpRequestMessage CreatePostRequest(Uri url, NameValueCollection data) =>
        new(HttpMethod.Post, url)
        {
            Content = new FormUrlEncodedContent(from i in Enumerable.Range(0, data.Count)
                                                from v in data.GetValues(i) ?? Enumerable.Empty<string>()
                                                select KeyValuePair.Create(data.GetKey(i), v))
        };

    public static IHttpQuery Submit(ParsedHtml html, string formSelector, NameValueCollection? data) =>
        WebLinq.HttpQuery.Submit(html, formSelector, null, null, data);

    public static IHttpQuery Submit(ParsedHtml html, int formIndex, NameValueCollection? data) =>
        WebLinq.HttpQuery.Submit(html, null, formIndex, null, data);

    public static IHttpQuery SubmitTo(Uri url, ParsedHtml html, string formSelector, NameValueCollection? data) =>
        WebLinq.HttpQuery.Submit(html, formSelector, null, url, data);

    public static IHttpQuery SubmitTo(Uri url, ParsedHtml html, int formIndex, NameValueCollection? data) =>
        WebLinq.HttpQuery.Submit(html, null, formIndex, url, data);

    public static IHttpQuery<TResult> For<T, TResult>(IEnumerable<T> source, Func<T, IHttpQuery<TResult>> querySelector) =>
        For(source, (item, _) => querySelector(item));

    public static IHttpQuery<TResult> For<T, TResult>(IEnumerable<T> source, Func<T, int, IHttpQuery<TResult>> querySelector)
    {
        return WebLinq.HttpQuery.Create((context, cancellationToken) =>
        {
            return Iterator();

            async IAsyncEnumerator<TResult> Iterator()
            {
                var i = 0;
                foreach (var item in source)
                {
                    var query = querySelector(item, i);

                    await foreach (var result in query.Share(context)
                                                      .WithCancellation(cancellationToken)
                                                      .ConfigureAwait(false))
                    {
                        yield return result;
                    }

                    checked { i++; }
                }
            }
        });
    }

    public static IHttpQuery<ImmutableArray<Cookie>> Cookies() =>
        WebLinq.HttpQuery.Create((context, cancellationToken) =>
        {
            var cookies = context.HttpClient.GetCookieContainer()?.GetAllCookies() is { } cks
                ? ImmutableArray.CreateRange(from ck in cks select new Cookie(ck))
                : ImmutableArray<Cookie>.Empty;

            return WebLinq.HttpQuery.Return(cookies).GetAsyncEnumerator(context, cancellationToken);
        });

    public static IHttpQuery Accept(this IHttpQuery query, params string[] mediaTypes) =>
        mediaTypes is [] ? query : query.Do(e =>
        {
            var c = new StringContent(string.Empty) { Headers = { ContentType = null } };
            foreach (var h in e.ContentHeaders)
                c.Headers.TryAddWithoutValidation(h.Key, h.Value.AsEnumerable());
            var headers = c.Headers;
            var actualMediaType = headers.ContentType?.MediaType;
            if (actualMediaType == null)
            {
                var contentDisposition = headers.ContentDisposition;
                var filename = contentDisposition?.FileName ?? contentDisposition?.FileNameStar;
                if (!string.IsNullOrEmpty(filename))
                    actualMediaType = MimeMapping.FindMimeTypeFromFileName(filename);
                if (actualMediaType == null)
                {
                    throw new UnacceptableMediaException($"Content has unspecified type when acceptable types are: {string.Join(", ", mediaTypes)}");
                }
            }

            Debug.Assert(mediaTypes != null);
            if (mediaTypes.Any(mediaType => string.Equals(mediaType, actualMediaType, StringComparison.OrdinalIgnoreCase)))
                return;

            throw new UnacceptableMediaException($"Unexpected content of type \"{actualMediaType}\". Acceptable types are: {string.Join(", ", mediaTypes)}");
        });

    internal static IHttpQuery Query(HttpFetcher fetcher) => new HttpQuery(fetcher);

    internal delegate Task<HttpRequestMessage> HttpFetcher(HttpQueryContext context, CancellationToken cancellationToken);

    sealed class HttpQuery : IHttpQuery
    {
        readonly IHttpQuery? _previous;
        readonly HttpFetcher _fetcher;

        public HttpQuery(HttpFetcher fetcher) : this(null, fetcher) { }

        public HttpQuery(IHttpQuery? previous, HttpFetcher fetcher) :
            this(HttpQuerySetup.Default, fetcher, previous) { }

        HttpQuery(HttpQuerySetup setup, HttpFetcher fetcher, IHttpQuery? previous)
        {
            Setup = setup;
            _fetcher = fetcher;
            _previous = previous;
        }

        HttpQuery(HttpQuery other) :
            this(other.Setup, other._fetcher, other._previous) { }

        public HttpQuerySetup Setup { get; private init; }

        public IHttpQuery WithSetup(HttpQuerySetup value) =>
            value == Setup ? this : new HttpQuery(this) { Setup = value };

        public IHttpQuery<HttpFetch<TContent>> ReadContent<TContent>(IHttpContentReader<TContent> reader) =>
            WebLinq.HttpQuery.Create((context, cancellationToken) =>
                GetAsyncEnumerator(context, reader, HttpFetch.Create, cancellationToken));

        public IAsyncEnumerator<HttpFetchInfo>
            GetAsyncEnumerator(HttpQueryContext context,
                               CancellationToken cancellationToken) =>
            GetAsyncEnumerator(context, HttpContentReader.None, (info, _) => info, cancellationToken);

        IAsyncEnumerator<TResult>
            GetAsyncEnumerator<T, TResult>(HttpQueryContext context,
                                           IHttpContentReader<T> contentReader,
                                           Func<HttpFetchInfo, T, TResult> resultSelector,
                                           CancellationToken cancellationToken)
        {
            return Iterator();

            async IAsyncEnumerator<TResult> Iterator()
            {
                if (_previous is { } previous)
                    await previous.WaitAsync(context, cancellationToken).ConfigureAwait(false);

                var request = await _fetcher(context, cancellationToken).ConfigureAwait(false);

                var id = context.NextId();
                var config = Setup.Configurer(context.Config);
                using var response = await SendAsync(context.HttpClient, config, request,
                                                     cancellationToken).ConfigureAwait(false);

                if (Setup.Options is null or { ReturnErroneousFetch: false })
                    response.EnsureSuccessStatusCode();

                Debug.Assert(request.RequestUri is not null);

                var info = new HttpFetchInfo(id, config,
                                             response.Version,
                                             response.StatusCode, response.ReasonPhrase,
                                             HttpHeaderCollection.Empty.Set(response.Headers),
                                             HttpHeaderCollection.Empty.Set(response.Content.Headers),
                                             request.RequestUri,
                                             HttpHeaderCollection.Empty.Set(request.Headers));

                if (!Setup.FilterPredicate(info))
                    yield break;

                var enumerator = contentReader.Read(info, response.Content, cancellationToken);
                await using (enumerator.ConfigureAwait(false))
                {
                    while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                        yield return resultSelector(info, enumerator.Current);
                }
            }
        }
    }

    static async Task<HttpResponseMessage> SendAsync(IHttpClient client,
                                                     HttpConfig config,
                                                     HttpRequestMessage request,
                                                     CancellationToken cancellationToken)
    {
        Debug.Assert(request.RequestUri is not null);

        var headers = request.Headers;

        var userAgent = headers.UserAgent;
        if (userAgent.Count is 0 && config.UserAgent.Length > 0)
            userAgent.ParseAdd(config.UserAgent);

        if (headers.Referrer is null
            && config.Headers.TryGetValue("Referer", out var configReferrer)
            && configReferrer.FirstOrDefault() is { } someReferrer)
        {
            headers.Referrer = new Uri(someReferrer);
        }

        if (headers.Accept.Count is 0 && config.Headers.TryGetValue("Accept", out var configAccept))
            headers.Accept.ParseAdd(configAccept);

        const string contentTypeHeaderName = "Content-Type";

        foreach (var (name, value) in
                 from e in config.Headers
                 where !e.Key.Equals(contentTypeHeaderName, StringComparison.OrdinalIgnoreCase)
                 from v in e.Value
                 select KeyValuePair.Create(e.Key, v))
        {
            if (!headers.TryAddWithoutValidation(name, value))
            {
#pragma warning disable CA2201 // Do not raise reserved exception types (FIXME?)
                throw new Exception($"Invalid HTTP header: {name}: {value}");
#pragma warning restore CA2201 // Do not raise reserved exception types
            }
        }

        if (request.Content is { } content
            && config.Headers.TryGetValue(contentTypeHeaderName, out var values))
        {
            foreach (var value in values)
            {
                if (!content.Headers.TryAddWithoutValidation(contentTypeHeaderName, value))
                {
#pragma warning disable CA2201 // Do not raise reserved exception types (FIXME?)
                    throw new Exception($"Invalid HTTP header: {contentTypeHeaderName}: {value}");
#pragma warning restore CA2201 // Do not raise reserved exception types
                }
            }
        }

        return await client.SendAsync(config, request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                           .ConfigureAwait(false);
    }
}
