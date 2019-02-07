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
    using System.Collections.Generic;

    public sealed class Map<TKey, TValue> : MapBase<TKey, TValue>
    {
        public static Map<TKey, TValue> Empty = new Map<TKey, TValue>(null);

        readonly Map<TKey, TValue> _link;

        public Map(IEqualityComparer<TKey> comparer) :
            base(comparer) {}

        public Map(Map<TKey, TValue> link, TKey key, TValue value, IEqualityComparer<TKey> comparer) :
            base(key, value, comparer ?? link?.Comparer) { _link = link; }

        public Map<TKey, TValue> WithComparer(IEqualityComparer<TKey> comparer) =>
            new Map<TKey, TValue>(_link, Key, Value, comparer);

        public Map<TKey, TValue> Set(KeyValuePair<TKey, TValue> pair) =>
            Set(pair.Key, pair.Value);

        public Map<TKey, TValue> Set(TKey key, TValue value) =>
            new Map<TKey, TValue>(this, key, value, null);

        public Map<TKey, TValue> Remove(TKey key) =>
            RemoveCore(Empty, key, (map, k, v) => map.Set(k, v));

        public override bool IsEmpty => _link == null;

        protected override IEnumerable<KeyValuePair<TKey, TValue>> Nodes =>
            GetNodesCore(this, m => m._link);
    }
}
