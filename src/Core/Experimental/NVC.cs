#region Copyright (c) 2017 Atif Aziz. All rights reserved.
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

namespace WebLinq.Experimental
{
    using System.Collections.Specialized;
    using System.Linq;
    using System.Reactive;
    using System.Text.RegularExpressions;

    // ReSharper disable once InconsistentNaming

    static class NVC
    {
        public static RW<NameValueCollection, int> Count() =>
            RW.Return((NameValueCollection c) => c.Count);

        public static RW<NameValueCollection, Unit> Clear() =>
            RW.Return((NameValueCollection c) => { c.Clear(); return new Unit(); });

        public static RW<NameValueCollection, string> GetPattern(string pattern) =>
            RW.Return((NameValueCollection c) =>
                          Enumerable
                              .Range(0, c.Count)
                              .Select(i => new { Index = i, Key = c.GetKey(i) })
                              .Where(e => Regex.IsMatch(e.Key, pattern))
                              .Select(e => c[e.Index])
                              .FirstOrDefault());

        public static RW<NameValueCollection, string> Get(string name) =>
            RW.Return((NameValueCollection c) => c[name]);

        public static RW<NameValueCollection, Unit> Nop() =>
            RW.Return((NameValueCollection c) => new Unit());

        public static RW<NameValueCollection, Unit> Put(string name, string value) =>
            RW.Return((NameValueCollection c) => { c[name] = value; return new Unit(); });
    }
}