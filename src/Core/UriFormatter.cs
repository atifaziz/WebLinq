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
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;

    static class UriFormatter
    {
        public static Uri Format(FormattableString formattableString)
            => formattableString == null
             ? throw new ArgumentNullException(nameof(formattableString))
             : new Uri((FormatStringParser.Parse(formattableString.Format,
                                                 (s, i, len) => s.HasWhiteSpace(i, len),
                                                 delegate { return false; })
                                          .Any(hws => hws)
                       ? FormattableStringFactory.Create(
                             string.Join(string.Empty, FormatStringParser.Parse(formattableString.Format, (s, i, len) => Regex.Replace(s.Substring(i, len), @"\s+", string.Empty), (s, i, len) => s.Substring(i, len))),
                             formattableString.GetArguments())
                       : formattableString).ToString(UriFormatProvider.InvariantCulture));
    }
}
