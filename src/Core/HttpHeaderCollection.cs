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
    #region Imports

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Http.Headers;
    using Collections;

    #endregion

    [DebuggerDisplay("Count = {Count}")]
    public sealed class HttpHeaderCollection : IReadOnlyDictionary<string, Strings>
    {
        public static readonly HttpHeaderCollection Empty = new();

        readonly ImmutableDictionary<string, Strings> _headers;

        public HttpHeaderCollection() :
            this(ImmutableDictionary.Create<string, Strings>(StringComparer.OrdinalIgnoreCase)) { }

        public HttpHeaderCollection(ImmutableDictionary<string, Strings> headers) =>
            _headers = headers;


        public HttpHeaderCollection Set(string key, Strings values) =>
            new(_headers.SetItem(key, values));

        internal HttpHeaderCollection Set(HttpHeaders headers) =>
            headers.Aggregate(this, (h, e) => h.Set(e.Key, Strings.Sequence(e.Value)));

        public HttpHeaderCollection Remove(string key) =>
            new(_headers.Remove(key));

        public int Count => _headers.Count;
        public bool ContainsKey(string key) => _headers.ContainsKey(key);
        public bool TryGetValue(string key, out Strings value) => _headers.TryGetValue(key, out value);
        public Strings this[string key] => _headers[key];
        public IEnumerable<string> Keys => _headers.Keys;
        public IEnumerable<Strings> Values => _headers.Values;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<KeyValuePair<string, Strings>> GetEnumerator() =>
            _headers.GetEnumerator();
    }
}
