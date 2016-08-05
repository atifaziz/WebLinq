// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace WebLinq.Collections
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    // Source: https://github.com/aspnet/HttpAbstractions/blob/cc295b1bd81c3cd54ed71eab87761e6acbf7a73e/src/Microsoft.AspNetCore.Http/HeaderDictionary.cs
    //
    // This is a slightly modified version from the snapshot above with the
    // following changes:
    //
    // - Renamed to StringsDictionary
    // - Moved from namespace Microsoft.AspNetCore.Http to one belonging
    //   to this project.
    // - Renamed from StringValues to Strings.
    // - Constructor taking a dictionary as external store rendered internal.

    public class StringsDictionary : IDictionary<string, Strings>
    {
#if NETSTANDARD1_3
        private static readonly string[] EmptyKeys = Array.Empty<string>();
        private static readonly Strings[] EmptyValues = Array.Empty<Strings>();
#else
        private static readonly string[] EmptyKeys = new string[0];
        private static readonly Strings[] EmptyValues = new Strings[0];
#endif
        private static readonly Enumerator EmptyEnumerator = new Enumerator();
        // Pre-box
        private static readonly IEnumerator<KeyValuePair<string, Strings>> EmptyIEnumeratorType = EmptyEnumerator;
        private static readonly IEnumerator EmptyIEnumerator = EmptyEnumerator;

        public StringsDictionary()
        {
        }

        internal StringsDictionary(Dictionary<string, Strings> store)
        {
            Store = store;
        }

        public StringsDictionary(int capacity)
        {
            Store = new Dictionary<string, Strings>(capacity, StringComparer.OrdinalIgnoreCase);
        }

        private Dictionary<string, Strings> Store { get; set; }

        /// <summary>
        /// Get or sets the associated value from the collection as a single string.
        /// </summary>
        /// <param name="key">The header name.</param>
        /// <returns>the associated value from the collection as a Strings or Strings.Empty if the key is not present.</returns>
        public Strings this[string key]
        {
            get
            {
                if (Store == null)
                {
                    return Strings.Empty;
                }

                Strings value;
                if (TryGetValue(key, out value))
                {
                    return value;
                }
                return Strings.Empty;
            }

            set
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                if (Strings.IsNullOrEmpty(value))
                {
                    Store?.Remove(key);
                }
                else
                {
                    if (Store == null)
                    {
                        Store = new Dictionary<string, Strings>(1, StringComparer.OrdinalIgnoreCase);
                    }

                    Store[key] = value;
                }
            }
        }

        /// <summary>
        /// Throws KeyNotFoundException if the key is not present.
        /// </summary>
        /// <param name="key">The header name.</param>
        /// <returns></returns>
        Strings IDictionary<string, Strings>.this[string key]
        {
            get { return Store[key]; }
            set { this[key] = value; }
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="StringsDictionary" />;.
        /// </summary>
        /// <returns>The number of elements contained in the <see cref="StringsDictionary" />.</returns>
        public int Count
        {
            get
            {
                return Store?.Count ?? 0;
            }
        }

        /// <summary>
        /// Gets a value that indicates whether the <see cref="StringsDictionary" /> is in read-only mode.
        /// </summary>
        /// <returns>true if the <see cref="StringsDictionary" /> is in read-only mode; otherwise, false.</returns>
        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public ICollection<string> Keys
        {
            get
            {
                if (Store == null)
                {
                    return EmptyKeys;
                }
                return Store.Keys;
            }
        }

        public ICollection<Strings> Values
        {
            get
            {
                if (Store == null)
                {
                    return EmptyValues;
                }
                return Store.Values;
            }
        }

        /// <summary>
        /// Adds a new list of items to the collection.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void Add(KeyValuePair<string, Strings> item)
        {
            if (item.Key == null)
            {
                throw new ArgumentNullException("The key is null");
            }
            if (Store == null)
            {
                Store = new Dictionary<string, Strings>(1, StringComparer.OrdinalIgnoreCase);
            }
            Store.Add(item.Key, item.Value);
        }

        /// <summary>
        /// Adds the given header and values to the collection.
        /// </summary>
        /// <param name="key">The header name.</param>
        /// <param name="value">The header values.</param>
        public void Add(string key, Strings value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (Store == null)
            {
                Store = new Dictionary<string, Strings>(1);
            }
            Store.Add(key, value);
        }

        /// <summary>
        /// Clears the entire list of objects.
        /// </summary>
        public void Clear()
        {
            Store?.Clear();
        }

        /// <summary>
        /// Returns a value indicating whether the specified object occurs within this collection.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>true if the specified object occurs within this collection; otherwise, false.</returns>
        public bool Contains(KeyValuePair<string, Strings> item)
        {
            Strings value;
            if (Store == null ||
                !Store.TryGetValue(item.Key, out value) ||
                !Strings.Equals(value, item.Value))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether the <see cref="StringsDictionary" /> contains a specific key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>true if the <see cref="StringsDictionary" /> contains a specific key; otherwise, false.</returns>
        public bool ContainsKey(string key)
        {
            if (Store == null)
            {
                return false;
            }
            return Store.ContainsKey(key);
        }

        /// <summary>
        /// Copies the <see cref="StringsDictionary" /> elements to a one-dimensional Array instance at the specified index.
        /// </summary>
        /// <param name="array">The one-dimensional Array that is the destination of the specified objects copied from the <see cref="StringsDictionary" />.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
        public void CopyTo(KeyValuePair<string, Strings>[] array, int arrayIndex)
        {
            if (Store == null)
            {
                return;
            }

            foreach (var item in Store)
            {
                array[arrayIndex] = item;
                arrayIndex++;
            }
        }

        /// <summary>
        /// Removes the given item from the the collection.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>true if the specified object was removed from the collection; otherwise, false.</returns>
        public bool Remove(KeyValuePair<string, Strings> item)
        {
            if (Store == null)
            {
                return false;
            }

            Strings value;

            if (Store.TryGetValue(item.Key, out value) && Strings.Equals(item.Value, value))
            {
                return Store.Remove(item.Key);
            }
            return false;
        }

        /// <summary>
        /// Removes the given header from the collection.
        /// </summary>
        /// <param name="key">The header name.</param>
        /// <returns>true if the specified object was removed from the collection; otherwise, false.</returns>
        public bool Remove(string key)
        {
            if (Store == null)
            {
                return false;
            }
            return Store.Remove(key);
        }

        /// <summary>
        /// Retrieves a value from the dictionary.
        /// </summary>
        /// <param name="key">The header name.</param>
        /// <param name="value">The value.</param>
        /// <returns>true if the <see cref="StringsDictionary" /> contains the key; otherwise, false.</returns>
        public bool TryGetValue(string key, out Strings value)
        {
            if (Store == null)
            {
                value = default(Strings);
                return false;
            }
            return Store.TryGetValue(key, out value);
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="Enumerator" /> object that can be used to iterate through the collection.</returns>
        public Enumerator GetEnumerator()
        {
            if (Store == null || Store.Count == 0)
            {
                // Non-boxed Enumerator
                return EmptyEnumerator;
            }
            return new Enumerator(Store.GetEnumerator());
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator<KeyValuePair<string, Strings>> IEnumerable<KeyValuePair<string, Strings>>.GetEnumerator()
        {
            if (Store == null || Store.Count == 0)
            {
                // Non-boxed Enumerator
                return EmptyIEnumeratorType;
            }
            return Store.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            if (Store == null || Store.Count == 0)
            {
                // Non-boxed Enumerator
                return EmptyIEnumerator;
            }
            return Store.GetEnumerator();
        }

        public struct Enumerator : IEnumerator<KeyValuePair<string, Strings>>
        {
            // Do NOT make this readonly, or MoveNext will not work
            private Dictionary<string, Strings>.Enumerator _dictionaryEnumerator;
            private bool _notEmpty;

            internal Enumerator(Dictionary<string, Strings>.Enumerator dictionaryEnumerator)
            {
                _dictionaryEnumerator = dictionaryEnumerator;
                _notEmpty = true;
            }

            public bool MoveNext()
            {
                if (_notEmpty)
                {
                    return _dictionaryEnumerator.MoveNext();
                }
                return false;
            }

            public KeyValuePair<string, Strings> Current
            {
                get
                {
                    if (_notEmpty)
                    {
                        return _dictionaryEnumerator.Current;
                    }
                    return default(KeyValuePair<string, Strings>);
                }
            }

            public void Dispose()
            {
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            void IEnumerator.Reset()
            {
                if (_notEmpty)
                {
                    ((IEnumerator)_dictionaryEnumerator).Reset();
                }
            }
        }
    }
}
