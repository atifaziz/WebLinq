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

namespace WebLinq.Text
{
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Reactive.Linq;
    using System.Text;
    using Mannex.Collections.Generic;

    public static class TextQuery
    {
        public static IObservable<string> Delimited<T>(this IObservable<T> query, string delimiter) =>
            query.Select((e, i) => i.AsKeyTo(e))
                 .Aggregate(
                    new StringBuilder(),
                    (sb, e) => sb.Append(e.Key > 0 ? delimiter : null).Append(e.Value),
                    sb => sb.ToString());

        public static IObservable<HttpFetch<string>> Text(this IHttpObservable query) =>
            query.ReadContent(f => f.Content.ReadAsStringAsync());

        public static IObservable<HttpFetch<string>> Text(this IHttpObservable query, Encoding encoding) =>
            query.ReadContent(async f =>
            {
                using (var stream = await f.Content.ReadAsStreamAsync().DontContinueOnCapturedContext())
                using (var reader = new StreamReader(stream, encoding))
                    return await reader.ReadToEndAsync().DontContinueOnCapturedContext();
            });

        public static IObservable<HttpFetch<string>> Text(this IObservable<HttpFetch<HttpContent>> query) =>
            from fetch in query
            from text in fetch.Content.ReadAsStringAsync()
            select fetch.WithContent(text);

        public static IObservable<HttpFetch<string>> Text(this IObservable<HttpFetch<HttpContent>> query, Encoding encoding) =>
            from fetch in query
            from bytes in fetch.Content.ReadAsByteArrayAsync()
            select fetch.WithContent(encoding.GetString(bytes));

        public static IContentObservable<string> Lines(this IHttpObservable query) =>
            Lines(query, null);

        public static IContentObservable<string> Lines(this IHttpObservable query, Encoding encoding) =>
            Lines(query, encoding, false);

        public static IContentObservable<string> Lines(this IHttpObservable query, Encoding encoding, bool force) =>
            query.ExpandContent(
                async f => new StreamReader(await f.Content.ReadAsStreamAsync(),
                                            encoding is Encoding e && force ? e : f.ContentCharSetEncoding ?? encoding),
                r => r,
                async r => await r.ReadLineAsync() is string line
                         ? (r, true, line)
                         : default);
    }
}
