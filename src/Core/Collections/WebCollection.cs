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
    using Mannex.Collections.Generic;

    /// <summary>
    /// An collection of key-value string pairs that maintains insertion order
    /// and allows looking up values by key (with duplicates allowed).
    /// </summary>

    public class WebCollection : ICollection<KeyValuePair<string, string>>
    {
        List<KeyValuePair<string, string>> _list;
        Dictionary<string, Strings> _cachedLookup;

        public List<KeyValuePair<string, string>> List =>
            _list ?? (_list = new List<KeyValuePair<string, string>>());

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => List.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public Strings? this[string key]
        {
            get
            {
                if (key == null) throw new ArgumentNullException(nameof(key));
                Strings value;
                return Cache.TryGetValue(key, out value) ? value : (Strings?) null;
            }
            set
            {
                if (key == null) throw new ArgumentNullException(nameof(key));
                InvalidateCachedLookup();
                var ii = -1;
                if (!IsEmpty)
                {
                    var i = 0;
                    var ss = default(OperationStatus);
                    while (FindKeyTailIndex(key, ref ss, ref i))
                    {
                        ii = i;
                        List.RemoveAt(i);
                    }
                }
                if (!value.HasValue)
                    return;
                if (ii < 0)
                    ii = List.Count;
                foreach (var s in value)
                    List.Insert(ii++, key.AsKeyTo(s));
            }
        }

        Dictionary<string, Strings> Cache => _cachedLookup ?? (_cachedLookup = CreateLookup());

        void InvalidateCachedLookup() { _cachedLookup = null; }

        Dictionary<string, Strings> CreateLookup()
        {
            var lookup = new Dictionary<string, Strings>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in this)
            {
                Strings values;
                if (!lookup.TryGetValue(e.Key, out values))
                    lookup.Add(e.Key, new Strings(e.Value));
                else
                    lookup[e.Key] = Strings.Concat(values, new Strings(e.Value));
            }
            return lookup;
        }

        public void Add(string key, string value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            Add(key.AsKeyTo(value));
        }

        public void Add(KeyValuePair<string, string> item)
        {
            if (item.Key == null) throw new ArgumentException(null, nameof(item));
            InvalidateCachedLookup();
            List.Add(item);
        }

        public void Clear()
        {
            InvalidateCachedLookup();
            _list = null;
        }

        int IndexOf(KeyValuePair<string, string> item)
        {
            if (item.Key == null) throw new ArgumentException(null, nameof(item));

            var count = Count;
            for (var i = 0; i < count; i++)
            {
                var e = List[i];
                if (StringComparer.OrdinalIgnoreCase.Equals(e.Key, item.Key) && e.Value == item.Value)
                    return i;
            }
            return -1;
        }

        public bool Contains(KeyValuePair<string, string> item) =>
            IndexOf(item) >= 0;

        public void CopyTo(KeyValuePair<string, string>[] array) =>
            CopyTo(array, 0);

        void ICollection<KeyValuePair<string, string>>.CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) =>
            CopyTo(array, arrayIndex);

        void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0 || arrayIndex > array.Length) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (IsEmpty)
                return;
            List.CopyTo(array, arrayIndex);
        }

        public bool Remove(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (IsEmpty)
                return false;
            var removed = false;
            var i = 0;
            var ss = default(OperationStatus);
            while (FindKeyTailIndex(key, ref ss, ref i))
            {
                removed = true;
                List.RemoveAt(i);
            }
            return removed;
        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            if (item.Key == null) throw new ArgumentException(null, nameof(item));
            var i = IndexOf(item);
            if (i < 0)
                return false;
            InvalidateCachedLookup();
            List.RemoveAt(i);
            return true;
        }

        bool IsEmpty => Count == 0;
        public int Count => List?.Count ?? 0;
        bool ICollection<KeyValuePair<string, string>>.IsReadOnly => false;

        bool FindKeyTailIndex(string key, ref OperationStatus state, ref int i) =>
            List.FindLastIndex(e => StringComparer.OrdinalIgnoreCase.Equals((string) e.Key, key), ref state, ref i);
    }
}
