#region Copyright (c) 2023 Atif Aziz. All rights reserved.
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

namespace WebLinq;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public interface IHttpContentReader<out T>
{
    IAsyncEnumerator<T> Read(HttpFetchInfo info, HttpContent content, CancellationToken cancellationToken);
}

public static class HttpContentReader
{
    public static readonly IHttpContentReader<HttpFetchInfo> None =
        Create(static (f, _, _) => Singleton(() => Task.FromResult(f)));

    public static IHttpContentReader<TResult> Select<T, TResult>(this IHttpContentReader<T> reader, Func<T, TResult> selector)
    {
        if (reader == null) throw new ArgumentNullException(nameof(reader));
        if (selector == null) throw new ArgumentNullException(nameof(selector));

        return Create((info, content, cancellationToken) =>
        {
            return Iterator();

            async IAsyncEnumerator<TResult> Iterator()
            {
                var enumerator = reader.Read(info, content, cancellationToken);
                await using (enumerator.ConfigureAwait(false))
                {
                    while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                        yield return selector(enumerator.Current);
                }
            }
        });
    }

    public static IHttpContentReader<byte[]> Bytes() =>
        Create(static (_, content, cancellationToken) => Singleton(() => content.ReadAsByteArrayAsync(cancellationToken)));

    public static IHttpContentReader<string> Text() =>
        Create(static (_, content, cancellationToken) => Singleton(() => content.ReadAsStringAsync(cancellationToken)));

    public static IHttpContentReader<string> Text(Encoding encoding) =>
        Create((_, content, cancellationToken) => Singleton(async () =>
        {
            var stream = await content.ReadAsStreamAsync(cancellationToken)
                                      .ConfigureAwait(false);
            await using (stream.ConfigureAwait(false))
            {
                var reader = new StreamReader(stream, encoding, leaveOpen: true); // disposed above
                return await reader.ReadToEndAsync().ConfigureAwait(false);
            }
        }));

    public static IHttpContentReader<string> Lines() => Lines(null);

    public static IHttpContentReader<string> Lines(Encoding? encoding) =>
        Create((_, content, cancellationToken) =>
        {
            return Iterator();

            async IAsyncEnumerator<string> Iterator()
            {
                if (encoding is null && content.Headers.ContentType?.CharSet is { } charSet)
                {
                    try
                    {
                        encoding = Encoding.GetEncoding(charSet);
                    }
                    catch (ArgumentException ex)
                    {
                        throw new InvalidOperationException($"Cannot read content as string using the invalid character set: {charSet}", ex);
                    }
                }

                var stream = await content.ReadAsStreamAsync(cancellationToken)
                                          .ConfigureAwait(false);
                await using (stream.ConfigureAwait(false))
                {
                    var reader = new StreamReader(stream, encoding, leaveOpen: true); // disposed above
                    while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        yield return line;
                    }
                }
            }
        });

    static async IAsyncEnumerator<T> Singleton<T>(Func<Task<T>> function)
    {
        yield return await function().ConfigureAwait(false);
    }

    public static IHttpContentReader<T> Create<T>(Func<HttpFetchInfo, HttpContent, CancellationToken, IAsyncEnumerator<T>> function) =>
        new HttpContentReader<T>(function);
}

sealed class HttpContentReader<T> : IHttpContentReader<T>
{
    readonly Func<HttpFetchInfo, HttpContent, CancellationToken, IAsyncEnumerator<T>> _delegatee;

    public HttpContentReader(Func<HttpFetchInfo, HttpContent, CancellationToken, IAsyncEnumerator<T>> delegatee) =>
        _delegatee = delegatee;

    public IAsyncEnumerator<T> Read(HttpFetchInfo info, HttpContent content, CancellationToken cancellationToken) =>
        _delegatee(info, content, cancellationToken);
}
