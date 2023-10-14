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
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mannex.Collections.Specialized;
using Mannex.Web;
using WebLinq.Html;

namespace WebLinq;

public interface IHttpQuery<out T> : IAsyncEnumerable<T>
{
    IAsyncEnumerator<T> GetAsyncEnumerator(HttpQueryContext context, CancellationToken cancellationToken);

    async IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken cancellationToken)
    {
        using var context = HttpQueryContext.CreateDefault();
        await foreach (var item in this.Share(context)
                                       .WithCancellation(cancellationToken)
                                       .ConfigureAwait(false))
        {
            yield return item;
        }
    }
}

public interface IHttpQuery : IHttpQuery<HttpFetchInfo>
{
    HttpQuerySetup Setup { get; }
    IHttpQuery WithSetup(HttpQuerySetup value);
    IHttpQuery<HttpFetch<TContent>> ReadContent<TContent>(IHttpContentReader<TContent> reader);
}

public partial class HttpQuery { }

static partial class HttpQuery
{
    public static IHttpQuery WithOptions(this IHttpQuery query, HttpOptions value)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        if (value == null) throw new ArgumentNullException(nameof(value));

        return query.WithSetup(query.Setup.WithOptions(value));
    }

    public static IHttpQuery WithConfigurer(this IHttpQuery query, Func<HttpConfig, HttpConfig> value)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        if (value == null) throw new ArgumentNullException(nameof(value));

        return query.WithSetup(query.Setup.WithConfigurer(value));
    }

    public static IHttpQuery WithFilterPredicate(this IHttpQuery query, Func<HttpFetchInfo, bool> value)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        if (value == null) throw new ArgumentNullException(nameof(value));

        return query.WithSetup(query.Setup.WithFilterPredicate(value));
    }

    public static IHttpQuery Configure(this IHttpQuery query, Func<HttpConfig, HttpConfig> modifier)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        if (modifier == null) throw new ArgumentNullException(nameof(modifier));

        return query.WithConfigurer(config => modifier(query.Setup.Configurer(config)));
    }

    public static IHttpQuery UserAgent(this IHttpQuery query, string value)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        if (value == null) throw new ArgumentNullException(nameof(value));

        return query.Configure(config => config.WithUserAgent(value));
    }

    public static IHttpQuery SetHeader(this IHttpQuery query, string name, string value)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        if (name == null) throw new ArgumentNullException(nameof(name));
        if (value == null) throw new ArgumentNullException(nameof(value));

        return query.Configure(config => config.WithHeader(name, value));
    }

    public static IHttpQuery ReturnErroneousFetch(this IHttpQuery query)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));

        return query.WithOptions(query.Setup.Options.WithReturnErroneousFetch(true));
    }

    public static IHttpQuery Filter(this IHttpQuery query, Func<HttpFetchInfo, bool> predicate)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        return query.WithFilterPredicate(f => query.Setup.FilterPredicate(f) && predicate(f));
    }

    public static IHttpQuery Do(this IHttpQuery query, Action<HttpFetchInfo> action) =>
        query.Filter(f => { action(f); return true; });

    public static IHttpQuery<T> Do<T>(this IHttpQuery<T> query, Action<T> action) =>
        Create((context, cancellationToken) =>
        {
            return Iterator();

            async IAsyncEnumerator<T> Iterator()
            {
                await foreach (var item in query.Share(context)
                                                .WithCancellation(cancellationToken)
                                                .ConfigureAwait(false))
                {
                    action(item);
                    yield return item;
                }
            }
        });

    public static IHttpQuery<TResult> Expose<T, TResult>(this IHttpQuery<T> query,
                                                         Func<IAsyncEnumerable<T>, IAsyncEnumerable<TResult>> selector) =>
        Create((context, cancellationToken) => selector(query.Share(context)).GetAsyncEnumerator(cancellationToken));

    public static IAsyncEnumerable<T> Share<T>(this IHttpQuery<T> query, HttpQueryContext context)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        if (context == null) throw new ArgumentNullException(nameof(context));

        return new ContextBoundQuery<T>(query, context);
    }

    sealed class ContextBoundQuery<T> : IAsyncEnumerable<T>
    {
        readonly IHttpQuery<T> _query;
        readonly HttpQueryContext _context;

        public ContextBoundQuery(IHttpQuery<T> query, HttpQueryContext context)
        {
            _query = query;
            _context = context;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken) =>
            _query.GetAsyncEnumerator(_context, cancellationToken);
    }

    internal static IHttpQuery Submit(this IHttpQuery<HttpFetch<ParsedHtml>> query,
                                      string? formSelector, int? formIndex,
                                      Uri? url, ISubmissionData<Unit> data) =>
        Flatten(from html in query
                select Submit(html.Content, formSelector, formIndex, url, _ => data));

    internal static IHttpQuery Submit(ParsedHtml html,
                                      string? formSelector, int? formIndex, Uri? actionUrl,
                                      NameValueCollection? data)
    {
        var submission = SubmissionData.Return(new Unit());

        if (data != null)
        {
            foreach (var e in data.AsEnumerable())
            {
                submission = submission.Do(fsc => fsc.Remove(e.Key));
                if (e.Value.Length == 1 && e.Value[0] == null)
                    continue;
                submission = e.Value.Aggregate(submission, (current, value) => current.Do(fsc => fsc.Add(e.Key, value)));
            }
        }

        return Submit(html, formSelector, formIndex, actionUrl, _ => submission);
    }

    internal static IHttpQuery Submit<T>(ParsedHtml html,
                                         string? formSelector, int? formIndex, Uri? actionUrl,
                                         Func<HtmlForm, ISubmissionData<T>> submissions)
    {
        Debug.Assert(formSelector is not null || formIndex is not null);

        var forms =
            from f in formIndex == null
                      ? html.QueryFormSelectorAll(formSelector)
                      : formIndex < html.Forms.Count
                      ? Enumerable.Repeat(html.Forms[(int) formIndex], 1)
                      : Enumerable.Empty<HtmlForm>()
            select new
            {
                Object = f,
                Action = new Uri(html.TryBaseHref(f.Action), UriKind.Absolute),
                // f.EncType, // TODO validate
                Data = f.GetSubmissionData(),
            };

        if (forms.FirstOrDefault() is not { } form)
            throw new ElementNotFoundException("No HTML form for submit.");

        submissions(form.Object).Run(form.Data);

        return form.Object.Method == HtmlFormMethod.Post
             ? Http.Post(actionUrl ?? form.Action, form.Data)
             : Http.Get(new UriBuilder(actionUrl ?? form.Action) { Query = form.Data.ToW3FormEncoded() }.Uri);
    }

    public static IHttpQuery<HttpFetch<T>> ExceptStatusCode<T>(this IHttpQuery<HttpFetch<T>> query, params HttpStatusCode[] statusCodes) =>
        query.Do(e =>
        {
            if (e.IsSuccessStatusCode || statusCodes.Any(sc => e.StatusCode == sc))
                return;
            (e.Content as IDisposable)?.Dispose();
            throw new HttpRequestException($"Response status code does not indicate success: {e.StatusCode}.");
        });
}

static partial class HttpQuery
{
    public static IHttpQuery<T> Empty<T>() => AsyncEnumerable.Empty<T>().ToHttpQuery();

    public static IHttpQuery<T> Return<T>(T value) =>
        Create((_, _) => AsyncEnumerableEx.Return(value));

    public static IHttpQuery<T> Create<T>(Func<HttpQueryContext, CancellationToken, IAsyncEnumerator<T>> function) =>
        new DelegatingHttpQuery<T>(function);

    public static IHttpQuery<TResult> Select<T, TResult>(this IHttpQuery<T> query, Func<T, TResult> selector) =>
        Create((context, cancellationToken) => query.Share(context)
                                                    .Select(selector)
                                                    .GetAsyncEnumerator(cancellationToken));

    public static IHttpQuery<TResult>
        SelectMany<TFirst, TSecond, TResult>(this IHttpQuery<TFirst> query,
                                             Func<TFirst, IHttpQuery<TSecond>> selector,
                                             Func<TFirst, TSecond, TResult> resultSelector) =>
        Create((context, cancellationToken) =>
        {
            return Iterator();

            async IAsyncEnumerator<TResult> Iterator()
            {
                var firstEnumerator = query.GetAsyncEnumerator(context, cancellationToken);
                await using (firstEnumerator.ConfigureAwait(false))
                {
                    while (await firstEnumerator.MoveNextAsync().ConfigureAwait(false))
                    {
                        var secondEnumerator = selector(firstEnumerator.Current).GetAsyncEnumerator(context, cancellationToken);
                        await using (secondEnumerator.ConfigureAwait(false))
                        {
                            while (await secondEnumerator.MoveNextAsync().ConfigureAwait(false))
                                yield return resultSelector(firstEnumerator.Current, secondEnumerator.Current);
                        }
                    }
                }
            }
        });

    public static IHttpQuery<TResult>
        SelectMany<TFirst, TSecond, TResult>(this IHttpQuery<TFirst> query,
                                             Func<TFirst, IAsyncEnumerable<TSecond>> selector,
                                             Func<TFirst, TSecond, TResult> resultSelector) =>
        Create((context, cancellationToken) => query.Share(context)
                                                    .SelectMany(selector, resultSelector)
                                                    .GetAsyncEnumerator(cancellationToken));

    public static IHttpQuery<TResult>
        SelectMany<TFirst, TSecond, TResult>(this IHttpQuery<TFirst> query,
                                             Func<TFirst, IEnumerable<TSecond>> selector,
                                             Func<TFirst, TSecond, TResult> resultSelector) =>
        Create((context, cancellationToken) => query.Share(context)
                                                    .SelectMany(e => selector(e).ToAsyncEnumerable()
                                                                                .ToHttpQuery(),
                                                                resultSelector)
                                                    .GetAsyncEnumerator(cancellationToken));

    public static IHttpQuery<T> Where<T>(this IHttpQuery<T> query, Func<T, bool> predicate) =>
        Create((context, cancellationToken) => query.Share(context)
                                                    .Where(predicate)
                                                    .GetAsyncEnumerator(cancellationToken));

    static IHttpQuery<T> ToHttpQuery<T>(this IAsyncEnumerable<T> source) =>
        Create((_, cancellationToken) =>
        {
            return Iterator();

            async IAsyncEnumerator<T> Iterator()
            {
                await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                    yield return item;
            }
        });

    public static IHttpQuery<(TFirst First, TSecond Second)>
        Zip<TFirst, TSecond>(this IHttpQuery<TFirst> first, IEnumerable<TSecond> second) =>
        first.Zip(second.ToAsyncEnumerable());

    public static IHttpQuery<(TFirst First, TSecond Second)>
        Zip<TFirst, TSecond>(this IHttpQuery<TFirst> first, IAsyncEnumerable<TSecond> second) =>
        first.Zip(second.ToHttpQuery());

    public static IHttpQuery<(TFirst First, TSecond Second)>
        Zip<TFirst, TSecond>(this IHttpQuery<TFirst> first, IHttpQuery<TSecond> second) =>
        Create((context, cancellationToken) => first.Share(context)
                                                    .Zip(second.Share(context))
                                                    .GetAsyncEnumerator(cancellationToken));

    public static async Task WaitAsync<T>(this IHttpQuery<T> query, CancellationToken cancellationToken)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));

        using var context = HttpQueryContext.CreateDefault();
        await query.WaitAsync(context, cancellationToken).ConfigureAwait(false);
    }

    public static async Task WaitAsync<T>(this IHttpQuery<T> query, HttpQueryContext context,
                                          CancellationToken cancellationToken)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        if (context == null) throw new ArgumentNullException(nameof(context));

        await foreach (var _ in query.Share(context)
                                     .WithCancellation(cancellationToken)
                                     .ConfigureAwait(false))
        {
            /* NOP */
        }
    }

    public static IHttpQuery<T> Content<T>(this IHttpQuery<HttpFetch<T>> query) =>
        from f in query
        select f.Content;

    public static IHttpQuery<HttpFetch<string>> Text(this IHttpQuery query)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));

        return query.ReadContent(HttpContentReader.Text());
    }

    public static IHttpQuery<HttpFetch<string>> Text(this IHttpQuery query, Encoding encoding)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        if (encoding == null) throw new ArgumentNullException(nameof(encoding));

        return query.ReadContent(HttpContentReader.Text(encoding));
    }

    public static IHttpQuery<HttpFetch<byte[]>> Bytes(this IHttpQuery query)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));

        return query.ReadContent(HttpContentReader.Bytes());
    }

    sealed class DelegatingHttpQuery<T> : IHttpQuery<T>
    {
        Func<HttpQueryContext, CancellationToken, IAsyncEnumerator<T>> _delegatee;

        public DelegatingHttpQuery(Func<HttpQueryContext, CancellationToken, IAsyncEnumerator<T>> delegatee) =>
            _delegatee = delegatee;

        public IAsyncEnumerator<T> GetAsyncEnumerator(HttpQueryContext context, CancellationToken cancellationToken) =>
            _delegatee(context, cancellationToken);
    }

    internal static IHttpQuery Flatten(this IHttpQuery<IHttpQuery> queryQuery) =>
        new HttpQueryChain(queryQuery);

    sealed class HttpQueryChain : IHttpQuery
    {
        readonly IHttpQuery<IHttpQuery> _queryQuery;

        public HttpQueryChain(IHttpQuery<IHttpQuery> queryQuery) :
            this(HttpQuerySetup.Default, queryQuery) { }

        HttpQueryChain(HttpQuerySetup setup, IHttpQuery<IHttpQuery> queryQuery)
        {
            Setup = setup;
            _queryQuery = queryQuery;
        }

        public HttpQuerySetup Setup { get; }

        public IHttpQuery WithSetup(HttpQuerySetup value) =>
            value == Setup ? this : new HttpQueryChain(value, _queryQuery);

        public IHttpQuery<HttpFetch<TContent>> ReadContent<TContent>(IHttpContentReader<TContent> reader) =>
            from query in _queryQuery
            from content in query.WithSetup(Setup).ReadContent(reader)
            select content;

        public IAsyncEnumerator<HttpFetchInfo>
            GetAsyncEnumerator(HttpQueryContext context,
                               CancellationToken cancellationToken)
        {
            var info = from f in ReadContent(HttpContentReader.None)
                       select f.Info;

            return info.GetAsyncEnumerator(context, cancellationToken);
        }
    }
}
