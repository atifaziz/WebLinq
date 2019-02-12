#region Copyright (c) 2019 Atif Aziz. All rights reserved.
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

    static class StringExtensions
    {
        public static (string, string) Split2(this string str, char delimiter)
        {
            if (str == null) throw new ArgumentNullException(nameof(str));
            var i = str.IndexOf(delimiter);
            return i >= 0
                 ? (str.Substring(0, i), str.Substring(i + 1))
                 : (str, null);
        }

        public static (string, string) Split2(this string str, string delimiter) =>
            Split2(str, delimiter, StringComparison.Ordinal);

        public static (string, string) Split2(this string str, string delimiter, StringComparison comparison)
        {
            if (str == null) throw new ArgumentNullException(nameof(str));
            var i = str.IndexOf(delimiter, comparison);
            return i >= 0
                 ? (str.Substring(0, i), str.Substring(i + delimiter.Length))
                 : (str, null);
        }

        public static bool HasWhiteSpace(this string str) =>
            HasWhiteSpace(str, 0, str.Length);

        public static bool HasWhiteSpace(this string str, int index, int length)
        {
            if (str == null) throw new ArgumentNullException(nameof(str));
            if (index < 0 || index >= str.Length) throw new ArgumentOutOfRangeException(nameof(index), index, null);
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), length, null);
            if (index + length > str.Length) throw new ArgumentOutOfRangeException(nameof(index), index, null);

            for (var i = index; i < length; i++)
            {
                if (char.IsWhiteSpace(str, i))
                    return true;
            }

            return false;
        }
    }
}
