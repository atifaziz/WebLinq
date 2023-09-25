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

namespace WebLinq
{
    using System;
    using System.Linq;

    struct ArrayList<T>
    {
        T[] _items;

        public int Count { get; private set; }

        public void EnsureCapacity(int capacity)
        {
            if (capacity <= (Capacity ?? 0))
                return;
            Array.Resize(ref _items, TwoPowers.RoundUpToClosest(capacity));
        }

        public T this[int index]
        {
            get { return _items[index]; }
            set
            {
                EnsureCapacity(index + 1);
                _items[index] = value;
                Count = Math.Max(Count, index + 1);
            }
        }

        public T this[int index, T defaultValue] =>
            index < Count ? this[index] : defaultValue;

        int? Capacity => _items?.Length;

        public T[] ToArray()
        {
            if (Count == 0)
                return Array.Empty<T>();
            Array.Resize(ref _items, Count);
            return _items;
        }
    }

    static class TwoPowers
    {
        static readonly int[] Cache = Enumerable.Range(0, 31).Select(p => 1 << p).ToArray();

        public static int RoundUpToClosest(int x)
        {
            if (x < 0 || x > Cache[^1])
                throw new ArgumentOutOfRangeException(nameof(x), x, null);
            var i = Array.BinarySearch(Cache, x);
            return Cache[i >= 0 ? i : ~i];
        }
    }
}
