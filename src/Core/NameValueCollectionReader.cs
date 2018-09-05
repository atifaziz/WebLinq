#region Copyright (c) 2017 Atif Aziz. All rights reserved.
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
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Reactive;
    using Mannex.Collections.Specialized;

    public interface IWebCollection : IEnumerable<KeyValuePair<string, string[]>>
    {
        int Count { get; }
        IReadOnlyCollection<string> Keys { get; }
        string this[string name] { get; set; }
        string this[int index] { get; }
        IReadOnlyCollection<string> GetValues(int index);
        IReadOnlyCollection<string> GetValues(string name);
        void Clear();
        void Add(string name, string value);
        void Remove(string name);
    }

    public delegate T WebCollectionComputer<out T>(IWebCollection collection);

    public static partial class WebCollection
    {
        internal static IWebCollection AsWebCollection(this NameValueCollection collection) =>
            new Collection(collection);

        public static WebCollectionComputer<T> Return<T>(T value) => _ => value;

        public static WebCollectionComputer<TResult> Bind<T, TResult>(this WebCollectionComputer<T> computer, Func<T, WebCollectionComputer<TResult>> selector) =>
            env => selector(computer(env))(env);

        public static WebCollectionComputer<TResult> Select<T, TResult>(this WebCollectionComputer<T> computer, Func<T, TResult> selector) =>
            env => selector(computer(env));

        public static WebCollectionComputer<TResult> SelectMany<TFirst, TSecond, TResult>(
            this WebCollectionComputer<TFirst> computer,
            Func<TFirst, WebCollectionComputer<TSecond>> secondSelector,
            Func<TFirst, TSecond, TResult> resultSelector) =>
            env =>
            {
                var t = computer(env);
                return resultSelector(t, secondSelector(t)(env));
            };

        public static WebCollectionComputer<IEnumerable<TResult>> For<T, TResult>(IEnumerable<T> source,
            Func<T, WebCollectionComputer<TResult>> f) =>
            coll => source.Select(f).Select(e => e(coll)).ToList();

        public static WebCollectionComputer<T> Do<T>(this WebCollectionComputer<T> computer, Action<IWebCollection> action) =>
            computer.Bind<T, T>(x => env => { action(env); return x; });

        public static WebCollectionComputer<Unit> Do(Action<IWebCollection> action) =>
            env => { action(env); return new Unit(); };

        sealed class Collection : IWebCollection
        {
            readonly NameValueCollection _collection;

            public Collection() : this(new NameValueCollection()) {}

            public Collection(NameValueCollection collection) =>
                _collection = collection ?? throw new ArgumentNullException(nameof(collection));

            public IEnumerator<KeyValuePair<string, string[]>> GetEnumerator() =>
                _collection.AsEnumerable().GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public int Count => _collection.Count;

            public IReadOnlyCollection<string> Keys => _collection.AllKeys;

            public string this[string name]
            {
                get => _collection[name];
                set => _collection[name] = value;
            }

            public string this[int index] => _collection[index];

            public IReadOnlyCollection<string> GetValues(int index) =>
                _collection.GetValues(index);

            public IReadOnlyCollection<string> GetValues(string name) =>
                _collection.GetValues(name);

            public void Add(IWebCollection c) =>
                _collection.Add((NameValueCollection) c);

            public void Clear() =>
                _collection.Clear();

            public void Add(string name, string value) =>
                _collection.Add(name, value);

            public void Remove(string name) =>
                _collection.Remove(name);
        }
    }
}

namespace WebLinq
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Mannex.Collections.Generic;
    using Mannex.Collections.Specialized;
    using Unit = System.Reactive.Unit;

    public static partial class WebCollection
    {
        public static WebCollectionComputer<IEnumerable<string>> Keys() =>
            coll => coll.Keys;

        public static WebCollectionComputer<string> Get(string key) =>
            coll => coll[key];

        public static WebCollectionComputer<Unit> Set(string key, string value) =>
            Do(coll => coll[key] = value);

        public static WebCollectionComputer<Unit> Set(IEnumerable<string> keys, string value) =>
            from _ in For(keys, k => Set(k, value))
            select new Unit();

        static WebCollectionComputer<string> TrySet(Func<IEnumerable<string>, string> matcher, string value) =>
            from ks in Keys()
            select matcher(ks) into k
            from r in k != null
                    ? from _ in Set(k, value) select k
                    : Return((string) null)
            select r;

        public static WebCollectionComputer<string> SetSingleWhere(Func<string, bool> matcher, string value) =>
            TrySet(ks => ks.Single(matcher), value);

        public static WebCollectionComputer<string> SetSingleMatching(string pattern, string value) =>
            SetSingleWhere(k => Regex.IsMatch(k, pattern), value);

        public static WebCollectionComputer<string> TrySetSingleWhere(Func<string, bool> matcher, string value) =>
            TrySet(ks => ks.SingleOrDefault(matcher), value);

        public static WebCollectionComputer<string> TrySetSingleMatching(string pattern, string value) =>
            TrySetSingleWhere(k => Regex.IsMatch(k, pattern), value);

        public static WebCollectionComputer<string> SetFirstWhere(Func<string, bool> matcher, string value) =>
            TrySet(ks => ks.First(matcher), value);

        public static WebCollectionComputer<string> TrySetFirstWhere(Func<string, bool> matcher, string value) =>
            TrySet(ks => ks.FirstOrDefault(matcher), value);

        public static WebCollectionComputer<string> TrySetFirstMatching(string pattern, string value) =>
            TrySetFirstWhere(k => Regex.IsMatch(k, pattern), value);

        public static WebCollectionComputer<IEnumerable<string>> SetWhere(Func<string, bool> matcher, string value) =>
            from ks in Keys()
            select ks.Where(matcher).ToArray() into ks
            from _ in Set(ks, value)
            select ks;

        public static WebCollectionComputer<IEnumerable<string>> SetMatching(string pattern, string value) =>
            SetWhere(k => Regex.IsMatch(k, pattern), value);

        public static WebCollectionComputer<Unit> Merge(NameValueCollection other) =>
            Do(coll =>
            {
                var entries = from e in other.AsEnumerable()
                              from v in e.Value select e.Key.AsKeyTo(v);
                foreach (var e in entries)
                    coll.Add(e.Key, e.Value);
            });

        public static WebCollectionComputer<NameValueCollection> Collect() =>
            coll => coll.SelectMany(e => e.Value, (e, v) => e.Key.AsKeyTo(v))
                        .ToNameValueCollection();

        public static WebCollectionComputer<Unit> Clear() =>
            coll => { coll.Clear(); return new Unit(); };
    }
}