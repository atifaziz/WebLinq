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
    using System.Collections.Generic;

    static class FormatStringParser
    {
        public static IEnumerable<T> Parse<T>(string format,
            Func<string, int, int, T> textSelector,
            Func<string, int, int, T> formatItemSelector)
        {
            if (format == null) throw new ArgumentNullException(nameof(format));
            if (textSelector == null) throw new ArgumentNullException(nameof(textSelector));
            if (formatItemSelector == null) throw new ArgumentNullException(nameof(formatItemSelector));

            var si = 0;
            var inFormatItem = false;
            for (var i = 0; i < format.Length; i++)
            {
                var ch = format[i];
                if (inFormatItem)
                {
                    if (ch == '}')
                    {
                        yield return formatItemSelector(format, si, i - si + 1);
                        si = i + 1;
                        inFormatItem = false;
                    }
                }
                else if (ch == '{')
                {
                    bool eoi;
                    if ((eoi = i + 1 == format.Length) || format[i + 1] != '{')
                    {
                        var len = i - si;
                        if (len > 0)
                            yield return textSelector(format, si, len);
                        if (eoi)
                            throw new FormatException($"Missing close delimiter '}}' in format string for format item starting with '{{' at offset {i}.");
                        si = i;
                        inFormatItem = true;
                    }
                    else
                    {
                        i++;
                    }
                }
                else if (ch == '}')
                {
                    if (i + 1 == format.Length || format[i + 1] != '}')
                        throw new FormatException($"Character '}}' at offset {i} must be escaped (by doubling) in a format string.");
                    i++;
                }
            }

            if (inFormatItem)
                throw new FormatException($"Missing close delimiter '}}' in format string for format item starting with '{{' at offset {si}.");

            if (si < format.Length)
                yield return textSelector(format, si, format.Length - si);
        }
    }
}
