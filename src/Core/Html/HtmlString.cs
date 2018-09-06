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

namespace WebLinq.Html
{
    using System;
    using System.Net;

    public struct HtmlString : IEquatable<HtmlString>
    {
        string _encoded;
        string _decoded;

        public static HtmlString FromEncoded(string encoded) => new HtmlString(encoded: encoded, decoded: null);
        public static HtmlString FromDecoded(string decoded) => new HtmlString(decoded: decoded, encoded: null);

        HtmlString(string encoded, string decoded)
        {
            _encoded = encoded;
            _decoded = decoded;
        }

        public string Decoded => _decoded ?? (_encoded == null ? null : (_decoded = WebUtility.HtmlDecode(_encoded)));
        public string Encoded => _encoded ?? (_decoded == null ? null : (_encoded = WebUtility.HtmlEncode(_decoded)));

        public bool Equals(HtmlString other) => string.Equals(Decoded, other.Decoded);
        public override bool Equals(object obj) => obj is HtmlString && Equals((HtmlString)obj);
        public override int GetHashCode() => Decoded?.GetHashCode() ?? 0;
        public override string ToString() => Encoded;
    }
}
