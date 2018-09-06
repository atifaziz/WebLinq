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

namespace WebLinq.Collections
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    // An immutable dictionary implementation that maintains a linked list
    // of entires. Each node in the linked list contains a cached dictionary
    // that is only built on first use, e.g. when a look is made or the
    // entries are iterated. The cache is built by walking the list nodes
    // from tail to head, with each node only being added if not already in
    // the dictionary. This way, newer nodes with the same key shadow older
    // ones.

    public abstract class MapBase<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    {
        protected TKey Key { get; }
        protected TValue Value { get; }
        public IEqualityComparer<TKey> Comparer { get; }
        Dictionary<TKey, TValue> _cache;

        protected MapBase(IEqualityComparer<TKey> comparer) :
            this(default(TKey), default(TValue), comparer) {}

        protected MapBase(TKey key, TValue value) :
            this(key, value, null) {}

        protected MapBase(TKey key, TValue value, IEqualityComparer<TKey> comparer)
        {
            Key      = key;
            Value    = value;
            Comparer = comparer ?? EqualityComparer<TKey>.Default;
        }

        bool HasCache => _cache != null;

        public Dictionary<TKey, TValue> Cache =>
            _cache ?? (_cache = CreateCache());

        protected virtual Dictionary<TKey, TValue> CreateCache()
        {
            var map = new Dictionary<TKey, TValue>(Comparer);
            foreach (var node in Nodes)
            {
                if (!map.ContainsKey(node.Key))
                    map.Add(node.Key, node.Value);
            }
            return map;
        }

        protected abstract IEnumerable<KeyValuePair<TKey, TValue>> Nodes { get; }

        protected static IEnumerable<KeyValuePair<TKey, TValue>> GetNodesCore<T>(T initial, Func<T, T> nextSelector)
            where T : MapBase<TKey, TValue>
        {
            if (initial == null) throw new ArgumentNullException(nameof(initial));
            if (nextSelector == null) throw new ArgumentNullException(nameof(nextSelector));
            for (var node = initial; !node.IsEmpty; node = nextSelector(node))
                yield return new KeyValuePair<TKey, TValue>(node.Key, node.Value);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() =>
            (IsEmpty ? Enumerable.Empty<KeyValuePair<TKey, TValue>>() : Cache).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public abstract bool IsEmpty { get; }

        public int Count => IsEmpty ? 0 : Cache.Count;

        public bool ContainsKey(TKey key) =>
            !IsEmpty && ((!HasCache && IsKey(key)) || Cache.ContainsKey(key));

        bool IsKey(TKey key) =>
            !IsEmpty && EqualityComparer<TKey>.Default.Equals(Key, key);

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (IsEmpty)
            {
                value = default(TValue);
                return false;
            }
            if (!HasCache && IsKey(key))
            {
                value = Value;
                return true;
            }
            return Cache.TryGetValue(key, out value);
        }

        public TValue TryGetValue(TKey key) =>
            TryGetValue(key, (found, value) => found ? value : default(TValue));

        public T TryGetValue<T>(TKey key, Func<bool, TValue, T> selector)
        {
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            TValue value;
            return TryGetValue(key, out value)
                 ? selector(true, value)
                 : selector(false, default(TValue));
        }

        public TValue this[TKey key]
        {
            get
            {
                if (IsEmpty) throw new KeyNotFoundException();
                return !HasCache && IsKey(key) ? Value : Cache[key];
            }
        }

        public IEnumerable<TKey> Keys => from e in this select e.Key;
        public IEnumerable<TValue> Values => from e in this select e.Value;

        protected T RemoveCore<T>(T initial, TKey key, Func<T, TKey, TValue, T> setter)
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var item in this)
            {
                if (!Comparer.Equals(key, item.Key))
                    initial = setter(initial, item.Key, item.Value);
            }
            return initial;
        }
    }
}
