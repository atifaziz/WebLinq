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
    using System.Collections.Generic;
    using System.Text;
    using Collections;

    enum OperationStatus { Initial, Running, Completed }

    static class Extensions
    {
        public static string ToQueryString(this WebCollection collection) =>
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

        public static string ToW3FormEncoded(this WebCollection collection) =>
            W3FormEncode(collection);

        static string W3FormEncode(WebCollection collection, string prefix = null)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));

            if (collection.Count == 0)
                return String.Empty;

            var sb = new StringBuilder();

            foreach (var e in collection)
            {
                var name = e.Key;
                var value = e.Value;

                if (sb.Length > 0)
                    sb.Append('&');

                sb.Append(Uri.EscapeDataString(name));

                if (value != null)
                    sb.Append('=');

                if (value != null)
                    sb.Append(Uri.EscapeDataString(value));
            }

            if (!string.IsNullOrEmpty(prefix) & sb.Length > 0)
                sb.Insert(0, prefix);

            return sb.ToString();
        }

        public static bool FindLastIndex<T>(this List<T> list, Predicate<T> predicate, ref OperationStatus state, ref int index)
        {
            if (state == OperationStatus.Initial)
            {
                index = list.Count - 1;
                state = OperationStatus.Running;
            }

            if (state == OperationStatus.Running)
            {
                if (index >= 0)
                    index = list.FindLastIndex(index, predicate);

                if (index < 0)
                    state = OperationStatus.Completed;
            }

            return state == OperationStatus.Running;
        }
    }
}