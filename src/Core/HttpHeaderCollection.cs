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
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics;

    [DebuggerDisplay("Count = {Count}")]
    public sealed class HttpHeaderCollection : NameValueCollection
    {
        public static readonly HttpHeaderCollection Empty = new HttpHeaderCollection();

        public HttpHeaderCollection() :
            base(0)
        {
            IsReadOnly = true;
        }

        public HttpHeaderCollection(NameValueCollection headers) :
            base(headers)
        {
            IsReadOnly = true;
        }

        public HttpHeaderCollection(IEnumerable<KeyValuePair<string, IEnumerable<string>>> entries) :
            base((entries as ICollection)?.Count ?? 8)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));

            foreach (var entry in entries)
                foreach (var value in entry.Value)
                    Add(entry.Key, value);
            IsReadOnly = true;
        }
    }
}