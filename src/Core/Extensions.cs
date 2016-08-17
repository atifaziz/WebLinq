#region Copyright (c) 2011 Atif Aziz. All rights reserved.
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
    using System.Text;
    using Collections;

    static class Extensions
    {
        public static string ToQueryString(this StringsDictionary collection) =>
            W3FormEncode(collection, "?");

        /// <summary>
        /// Encodes the content of the collection to a string
        /// suitably formatted per the <c>application/x-www-form-urlencoded</c>
        /// MIME media type.
        /// </summary>
        /// <remarks>
        /// Each value is escaped using <see cref="Uri.EscapeDataString"/>
        /// but which can throw <see cref="UriFormatException"/> for very
        /// large values.
        /// </remarks>

        public static string ToW3FormEncoded(this StringsDictionary collection) =>
            W3FormEncode(collection);

        static string W3FormEncode(StringsDictionary collection, string prefix = null)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));

            if (collection.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();

            foreach (var e in collection)
            {
                var name = e.Key;
                var values = e.Value;

                if (values.Count > 0)
                {
                    foreach (var value in values)
                    {
                        if (sb.Length > 0)
                            sb.Append('&');

                        sb.Append(Uri.EscapeDataString(name)).Append('=');

                        if (!string.IsNullOrEmpty(value))
                            sb.Append(Uri.EscapeDataString(value));
                    }
                }
                else
                {
                    if (sb.Length > 0)
                        sb.Append('&');
                    sb.Append(name);
                }
            }

            if (!string.IsNullOrEmpty(prefix) & sb.Length > 0)
                sb.Insert(0, prefix);

            return sb.ToString();
        }
    }
}