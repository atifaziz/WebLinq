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

namespace WebLinq
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Unit = System.Reactive.Unit;

    public static class NameValueCollectionReader
    {
        public static Reader<NameValueCollection, T> Return<T>(T value) =>
            _ => value;

        public static Reader<NameValueCollection, IEnumerable<string>> Keys() =>
            coll => coll.Keys.Cast<string>();

        public static Reader<NameValueCollection, string> Get(string key) =>
            coll => coll[key];

        public static Reader<NameValueCollection, Unit> Set(string key, string value) =>
            Reader.Do<NameValueCollection>(coll => coll[key] = value);

        public static Reader<NameValueCollection, Unit> Set(IEnumerable<string> keys, string value) =>
            from _ in Reader.For(keys, k => Set(k, value))
            select new Unit();

        static Reader<NameValueCollection, string> TrySet(Func<IEnumerable<string>, string> matcher, string value) =>
            from ks in Keys()
            select matcher(ks) into k
            from r in k != null
                    ? from _ in Set(k, value) select k
                    : Return((string) null)
            select r;

        public static Reader<NameValueCollection, string> SetSingleWhere(Func<string, bool> matcher, string value) =>
            TrySet(ks => ks.Single(matcher), value);

        public static Reader<NameValueCollection, string> SetSingleMatching(string pattern, string value) =>
            SetSingleWhere(k => Regex.IsMatch(k, pattern), value);

        public static Reader<NameValueCollection, string> TrySetSingleWhere(Func<string, bool> matcher, string value) =>
            TrySet(ks => ks.SingleOrDefault(matcher), value);

        public static Reader<NameValueCollection, string> TrySetSingleMatching(string pattern, string value) =>
            TrySetSingleWhere(k => Regex.IsMatch(k, pattern), value);

        public static Reader<NameValueCollection, string> SetFirstWhere(Func<string, bool> matcher, string value) =>
            TrySet(ks => ks.First(matcher), value);

        public static Reader<NameValueCollection, string> TrySetFirstWhere(Func<string, bool> matcher, string value) =>
            TrySet(ks => ks.FirstOrDefault(matcher), value);

        public static Reader<NameValueCollection, string> TrySetFirstMatching(string pattern, string value) =>
            TrySetFirstWhere(k => Regex.IsMatch(k, pattern), value);

        public static Reader<NameValueCollection, IEnumerable<string>> SetWhere(Func<string, bool> matcher, string value) =>
            from ks in Keys()
            select ks.Where(matcher).ToArray() into ks
            from _ in Set(ks, value)
            select ks;

        public static Reader<NameValueCollection, IEnumerable<string>> SetMatching(string pattern, string value) =>
            SetWhere(k => Regex.IsMatch(k, pattern), value);

        public static Reader<NameValueCollection, Unit> Merge(NameValueCollection other) =>
            Reader.Do<NameValueCollection>(coll => coll.Add(other));

        public static Reader<NameValueCollection, NameValueCollection> Collect() =>
            coll => new NameValueCollection(coll);

        public static Reader<NameValueCollection, Unit> Clear() =>
            coll => { coll.Clear(); return new Unit(); };
    }
}