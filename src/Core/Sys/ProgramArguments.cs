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

namespace WebLinq.Sys
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Text;

    // A union of process arguments stored as either a list or a single
    // string representing the arguments in a command-line.

    public sealed partial class ProgramArguments : ICollection<string>
    {
        public static readonly ProgramArguments Empty = new ProgramArguments(null, ImmutableArray<string>.Empty);

        readonly string _line;
        List<string> _parsedLineArgs;
        readonly ImmutableArray<string> _args;

        ProgramArguments(string line, ImmutableArray<string> args)
        {
            Debug.Assert(args.IsDefault || line == null);
            _line = line;
            _args = args;
        }

        public static ProgramArguments Parse(string args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));
            return args.Length == 0 ? Empty : new ProgramArguments(args, default);
        }

        public static ProgramArguments Var(params string[] args) =>
            From(args);

        public static ProgramArguments From(IEnumerable<string> args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));
            return new ProgramArguments(null, ImmutableArray.CreateRange(args));
        }

        ICollection<string> Args
            => _line is string line
             ? line.Length == 0 ? (ICollection<string>)Array.Empty<string>()
             : _parsedLineArgs ?? (_parsedLineArgs = ParseArgumentsIntoList(line))
             : _args;

        public int Count => _line is string s && s.Length == 0 ? 0 : Args.Count;

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();

        public IEnumerator<string> GetEnumerator() =>
            Args.GetEnumerator();

        public override string ToString()
            => Count == 0 ? string.Empty : _line ?? PasteArguments.Paste(_args);

        bool ICollection<string>.IsReadOnly => true;

        bool ICollection<string>.Contains(string item) => Args.Contains(item);
        void ICollection<string>.CopyTo(string[] array, int arrayIndex) => Args.CopyTo(array, arrayIndex);

        void ICollection<string>.Add(string item) => throw new NotSupportedException();
        void ICollection<string>.Clear() => throw new NotSupportedException();
        bool ICollection<string>.Remove(string item) => throw new NotSupportedException();
    }

    partial class ProgramArguments
    {
        static List<string> ParseArgumentsIntoList(string arguments)
        {
            var list = new List<string>();
            ParseArgumentsIntoList(arguments, list);
            return list;
        }

        #region Copyright (c) .NET Foundation and Contributors
        //
        // The MIT License (MIT)
        //
        // All rights reserved.
        //
        // Permission is hereby granted, free of charge, to any person obtaining a copy
        // of this software and associated documentation files (the "Software"), to deal
        // in the Software without restriction, including without limitation the rights
        // to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        // copies of the Software, and to permit persons to whom the Software is
        // furnished to do so, subject to the following conditions:
        //
        // The above copyright notice and this permission notice shall be included in all
        // copies or substantial portions of the Software.
        //
        // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        // FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
        // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
        // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
        // SOFTWARE.
        //
        #endregion

        // https://github.com/dotnet/corefx/blob/96925bba1377b6042fc7322564b600a4e651f7fd/src/System.Diagnostics.Process/src/System/Diagnostics/Process.Unix.cs#L527-L626

        /// <summary>Parses a command-line argument string into a list of arguments.</summary>
        /// <param name="arguments">The argument string.</param>
        /// <param name="results">The list into which the component arguments should be stored.</param>
        /// <remarks>
        /// This follows the rules outlined in "Parsing C++ Command-Line Arguments" at
        /// https://msdn.microsoft.com/en-us/library/17w5ykft.aspx.
        /// </remarks>
        private static void ParseArgumentsIntoList(string arguments, List<string> results)
        {
            // Iterate through all of the characters in the argument string.
            for (int i = 0; i < arguments.Length; i++)
            {
                while (i < arguments.Length && (arguments[i] == ' ' || arguments[i] == '\t'))
                    i++;

                if (i == arguments.Length)
                    break;

                results.Add(GetNextArgument(ref i));
            }

            string GetNextArgument(ref int i)
            {
                var currentArgument = StringBuilderCache.Acquire();
                bool inQuotes = false;

                while (i < arguments.Length)
                {
                    // From the current position, iterate through contiguous backslashes.
                    int backslashCount = 0;
                    while (i < arguments.Length && arguments[i] == '\\')
                    {
                        i++;
                        backslashCount++;
                    }

                    if (backslashCount > 0)
                    {
                        if (i >= arguments.Length || arguments[i] != '"')
                        {
                            // Backslashes not followed by a double quote:
                            // they should all be treated as literal backslashes.
                            currentArgument.Append('\\', backslashCount);
                        }
                        else
                        {
                            // Backslashes followed by a double quote:
                            // - Output a literal slash for each complete pair of slashes
                            // - If one remains, use it to make the subsequent quote a literal.
                            currentArgument.Append('\\', backslashCount / 2);
                            if (backslashCount % 2 != 0)
                            {
                                currentArgument.Append('"');
                                i++;
                            }
                        }

                        continue;
                    }

                    char c = arguments[i];

                    // If this is a double quote, track whether we're inside of quotes or not.
                    // Anything within quotes will be treated as a single argument, even if
                    // it contains spaces.
                    if (c == '"')
                    {
                        if (inQuotes && i < arguments.Length - 1 && arguments[i + 1] == '"')
                        {
                            // Two consecutive double quotes inside an inQuotes region should result in a literal double quote
                            // (the parser is left in the inQuotes region).
                            // This behavior is not part of the spec of code:ParseArgumentsIntoList, but is compatible with CRT
                            // and .NET Framework.
                            currentArgument.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = !inQuotes;
                        }

                        i++;
                        continue;
                    }

                    // If this is a space/tab and we're not in quotes, we're done with the current
                    // argument, it should be added to the results and then reset for the next one.
                    if ((c == ' ' || c == '\t') && !inQuotes)
                    {
                        break;
                    }

                    // Nothing special; add the character to the current argument.
                    currentArgument.Append(c);
                    i++;
                }

                return StringBuilderCache.GetStringAndRelease(currentArgument);
            }
        }
    }
}
