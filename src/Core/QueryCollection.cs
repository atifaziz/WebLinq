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
    using MoreLinq;
    using Collections;

    public partial class QueryCollection : IReadOnlyCollection<KeyValuePair<string, string>>
    {
        public static readonly QueryCollection Empty = new QueryCollection();

        ImmutableArray<KeyValuePair<string, string>> _entries;
        (bool, IReadOnlyCollection<string>) _keys;
        (bool, ImmutableDictionary<string, Strings>) _dictionary;

        static readonly Enumerator EmptyEnumerator = new Enumerator();
        static readonly IEnumerator<KeyValuePair<string, string>> BoxedEmptyEnumerator = EmptyEnumerator;

        ImmutableDictionary<string, Strings> Dictionary
            => Count > 4
             ? this.LazyGet(ref _dictionary,
                            it => ImmutableDictionary.CreateRange(StringComparer.OrdinalIgnoreCase,
                                                                  it.Groups))
             : null;

        QueryCollection() {}

        public QueryCollection(ImmutableArray<KeyValuePair<string, string>> entries) =>
            _entries = entries;

        public QueryCollection(QueryCollection collection)
        {
            _entries = collection._entries;
            _keys = collection._keys;
            _dictionary = collection._dictionary;
        }

        ImmutableArray<string>.Builder _arrayBuilder;

        ImmutableArray<string>.Builder ArrayBuilder =>
            _arrayBuilder ?? (_arrayBuilder = ImmutableArray.CreateBuilder<string>(0));

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
             : this.LazyGet(ref _keys,
                            it => ImmutableArray.CreateRange(it.Select(e => e.Key)
                                                               .Distinct(StringComparer.OrdinalIgnoreCase)));

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

        (bool, ImmutableArray<KeyValuePair<string, Strings>>) _groups;

        public ImmutableArray<KeyValuePair<string, Strings>> Groups =>
            this.LazyGet(ref _groups,
                         it => it._entries
                                 .GroupBy(e => e.Key,
                                          e => e.Value,
                                          (k, vs) => KeyValuePair.Create(k, Strings.Sequence(vs)),
                                          StringComparer.OrdinalIgnoreCase)
                                 .ToImmutableArray());

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
            if (Count > 0)
            {
                var dict = Dictionary;
                if (dict != null)
                    return dict.TryGetValue(key, out value);

                var values = Strings.Empty;
                var count = 0;

                foreach (var (k, v) in _entries)
                {
                    if (string.Equals(k, key, StringComparison.OrdinalIgnoreCase))
                    {
                        if (count == 0)
                            values = v;
                        count++;
                    }
                }

                if (count == 1)
                {
                    value = values;
                    return true;
                }

                if (count > 1)
                {
                    var array = ArrayBuilder;
                    array.Capacity = count;

                    foreach (var (k, v) in _entries)
                    {
                        if (string.Equals(k, key, StringComparison.OrdinalIgnoreCase))
                            array.Add(v);
                    }

                    value = new Strings(array.MoveToImmutable());
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

        IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator()
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

        public struct Enumerator : IEnumerator<KeyValuePair<string, string>>
        {
            // Do NOT make this readonly, or MoveNext will not work
            ImmutableArray<KeyValuePair<string, string>>.Enumerator _enumerator;
            readonly bool _notEmpty;

            internal Enumerator(ImmutableArray<KeyValuePair<string, string>>.Enumerator enumerator)
            {
                _enumerator = enumerator;
                _notEmpty = true;
            }

            public bool MoveNext() =>
                _notEmpty && _enumerator.MoveNext();

            public KeyValuePair<string, string> Current =>
                _notEmpty ? _enumerator.Current : default;

            public void Dispose() {}

            object IEnumerator.Current => Current;

            void IEnumerator.Reset() => throw new NotSupportedException();
        }
    }

    partial class QueryCollection
    {
        public QueryCollection Merge(IEnumerable<KeyValuePair<string, string>> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            source = source.ToArray();

            var join =
                Groups.GroupJoin(source, e => e.Key,
                                         e => e.Key,
                                         (a, b) => KeyValuePair.Create(a.Key, a.Value.Concat(from e in b select e.Value)),
                                         StringComparer.OrdinalIgnoreCase)
                      .SelectMany(e => from v in e.Value
                                       select KeyValuePair.Create(e.Key, v))
                      .ToArray();

            var array = ImmutableArray.CreateBuilder<KeyValuePair<string, string>>();
            array.AddRange(join);
            array.AddRange(source.ExceptBy(join, e => e.Key, StringComparer.OrdinalIgnoreCase));
            return new QueryCollection(array.ToImmutable());
        }
    }
}
