// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Source:
// https://github.com/aspnet/AspNetCore/blob/574be0d22c1678ed5f6db990aec78b4db587b267/src/Http/Http/src/Internal/QueryCollection.cs
//
// This is a modified version from the snapshot above with the following changes:
//
// - Moved from namespace Microsoft.AspNetCore.Http.Internal to one belonging
//   to this project.
// - Renamed from StringValues to Strings.
// - Re-styled to use project conventions.
// - Implementation revised to maintain entry order.

namespace WebLinq
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Collections;

    public class QueryCollection : IReadOnlyCollection<KeyValuePair<string, Strings>>
    {
        public static readonly QueryCollection Empty = new QueryCollection();

        ImmutableArray<KeyValuePair<string, Strings>> _entries;
        (bool, IReadOnlyCollection<string>) _keys;
        (bool, ImmutableDictionary<string, Strings>) _dictionary;

        static readonly Enumerator EmptyEnumerator = new Enumerator();
        static readonly IEnumerator<KeyValuePair<string, Strings>> BoxedEmptyEnumerator = EmptyEnumerator;

        ImmutableDictionary<string, Strings> Dictionary
            => Count > 4
             ? this.LazyGet(ref _dictionary, it => ImmutableDictionary.CreateRange(it._entries))
             : null;

        QueryCollection() {}

        public QueryCollection(ImmutableArray<KeyValuePair<string, Strings>> entries) =>
            _entries = entries;

        public QueryCollection(QueryCollection collection)
        {
            _entries = collection._entries;
            _dictionary = collection._dictionary;
        }

        /// <summary>
        /// Get or sets the associated value from the collection as a single
        /// string.
        /// </summary>
        /// <param name="key">The key name.</param>
        /// <returns>
        /// The associated value from the collection as a <see cref="Strings"/>
        /// or <see cref="Strings.Empty"/> if the key is not present.
        /// </returns>

        public Strings this[string key]
            => _entries == null ? Strings.Empty
             : TryGetValue(key, out var value) ? value
             : Strings.Empty;

        /// <summary>
        /// Gets the number of elements contained in the
        /// <see cref="QueryCollection" />.
        /// </summary>
        /// <returns>The number of elements contained in the
        /// <see cref="QueryCollection" />.</returns>

        public int Count => _entries.Length;

        public IReadOnlyCollection<string> Keys
            => Count == 0
             ? Array.Empty<string>()
             : this.LazyGet(ref _keys, it => ImmutableArray.CreateRange(from e in it select e.Key));

        /// <summary>
        /// Determines whether the <see cref="QueryCollection" /> contains a
        /// specific key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>
        /// Boolean true if the <see cref="QueryCollection" /> contains
        /// a specific key; otherwise, false.</returns>

        public bool ContainsKey(string key)
        {
            if (Dictionary != null)
                return Dictionary.ContainsKey(key);

            foreach (var e in _entries)
            {
                if (string.Equals(e.Key, key, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Retrieves a value from the collection.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// Boolean true if the <see cref="QueryCollection" /> contains the key;
        /// otherwise, false.</returns>

        public bool TryGetValue(string key, out Strings value)
        {
            if (Count == 0)
            {
                value = default;
                return false;
            }

            var dict = Dictionary;
            if (dict != null)
                return dict.TryGetValue(key, out value);

            foreach (var e in _entries)
            {
                if (string.Equals(e.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    value = e.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        QueryString ToQueryString() => QueryString.Create(this);

        public override string ToString() =>
            ToQueryString().ToString();

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="Enumerator" /> object that can be used to  iterate
        /// through the collection.</returns>

        public Enumerator GetEnumerator()
            => Count == 0
             ? EmptyEnumerator // Non-boxed Enumerator
             : new Enumerator(_entries.GetEnumerator());

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerator{T}" /> object that can be used to iterate
        /// through the collection.</returns>

        IEnumerator<KeyValuePair<string, Strings>> IEnumerable<KeyValuePair<string, Strings>>.GetEnumerator()
            => Count == 0
             ? BoxedEmptyEnumerator // Non-boxed Enumerator
             : new Enumerator(_entries.GetEnumerator());

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerator" /> object that can be used to iterate
        /// through the collection.</returns>

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Count == 0 ? BoxedEmptyEnumerator : _(); IEnumerator _()
            {
                foreach (var e in _entries)
                    yield return e;
            }
        }

        public struct Enumerator : IEnumerator<KeyValuePair<string, Strings>>
        {
            // Do NOT make this readonly, or MoveNext will not work
            ImmutableArray<KeyValuePair<string, Strings>>.Enumerator _enumerator;
            readonly bool _notEmpty;

            internal Enumerator(ImmutableArray<KeyValuePair<string, Strings>>.Enumerator enumerator)
            {
                _enumerator = enumerator;
                _notEmpty = true;
            }

            public bool MoveNext() =>
                _notEmpty && _enumerator.MoveNext();

            public KeyValuePair<string, Strings> Current =>
                _notEmpty ? _enumerator.Current : default;

            public void Dispose() {}

            object IEnumerator.Current => Current;

            void IEnumerator.Reset() => throw new NotSupportedException();
        }
    }
}
