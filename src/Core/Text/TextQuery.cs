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
    using System.Collections.Generic;
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
            ContentObservable.Create<string>((options, observer) =>
                query.ExpandContent(
                    async f => new StreamReader(await f.Content.ReadAsStreamAsync(), encoding is Encoding e && force ? e : f.ContentCharSetEncoding ?? encoding),
                    r => (Reader: r, Count: 0),
                    async s => { Console.WriteLine("here"); return await s.Reader.ReadLineAsync() is string line && options.FilterPredicate(line, s.Count) ? ((s.Reader, s.Count + 1), true, line) : default; }).Subscribe(observer));
    }

    public sealed class ContentOptions<T>
    {
        public static readonly ContentOptions<T> Default = new ContentOptions<T>(delegate { return true; });

        public Func<T, int, bool> FilterPredicate { get; private set; }

        ContentOptions(Func<T, int, bool> filterPredicate) =>
            FilterPredicate = filterPredicate;

        ContentOptions(ContentOptions<T> other) :
            this(other.FilterPredicate)  {}

        public ContentOptions<T> WithFilterPredicate(Func<T, int, bool> value) =>
            FilterPredicate == value ? this : new ContentOptions<T>(this) { FilterPredicate = value };
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
            Options = options ?? throw new ArgumentNullException(nameof(options));
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

        public static IContentObservable<T> Where<T>(this IContentObservable<T> source, Func<T, int, bool> predicate) =>
            source.WithOptions(source.Options.WithFilterPredicate((e, i) => source.Options.FilterPredicate(e, i) && predicate(e, i)));

        public static IContentObservable<T> Take<T>(this IContentObservable<T> source, int count) =>
            source.Where((e, i) => i < count);
    }
}
