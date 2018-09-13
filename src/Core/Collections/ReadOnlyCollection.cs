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
    using System.Collections;
    using System.Collections.Generic;

    static class ReadOnlyCollection
    {
        public static IReadOnlyCollection<T> Singleton<T>(T item) =>
            new Collection<T>(item);

        sealed class Collection<T> : IReadOnlyCollection<T>
        {
            readonly T _item;
            public Collection(T item) { _item = item; }
            public IEnumerator<T> GetEnumerator() { yield return _item; }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public int Count => 1;
        }
    }

    static class ReadOnlyCollection<T>
    {
        public static IReadOnlyCollection<T> Empty = new EmptyCollection();

        sealed class EmptyCollection : IReadOnlyCollection<T>
        {
            public IEnumerator<T> GetEnumerator() { yield break; }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public int Count => 0;
        }
    }
}
