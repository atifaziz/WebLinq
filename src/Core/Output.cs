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
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using Mannex.Collections.Generic;

    public sealed class CsvOutputQueryBuilder<T>
    {
        readonly IQuery<T> _query;
        List<KeyValuePair<string, Func<T, object>>> _fields;

        internal CsvOutputQueryBuilder(IQuery<T> query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            _query = query;
        }

        bool HasFields => _fields?.Count > 0;

        ICollection<KeyValuePair<string, Func<T, object>>> Fields =>
            _fields ?? (_fields = new List<KeyValuePair<string, Func<T, object>>>());

        public CsvOutputQueryBuilder<T> Field(string name, Func<T, object> selector)
        {
            Fields.Add(name.AsKeyTo(selector));
            return this;
        }

        static readonly IEnumerable<KeyValuePair<string, Func<T, object>>>
            SingletonField =
                new[] { "Item".AsKeyTo(new Func<T, object>(e => e)) };

        public CsvOutputQueryBuilder<T> Reflect() => Reflect(s => s);

        public CsvOutputQueryBuilder<T> Reflect(Func<string, string> nameMapper)
        {
            if (nameMapper == null) throw new ArgumentNullException(nameof(nameMapper));

            var type = typeof(T);

            var fields =
                Type.GetTypeCode(type) != TypeCode.Object
                    ? SingletonField
                    : from props in new[] { type.GetProperties(BindingFlags.Public | BindingFlags.Instance) }
                      from e in
                          props.Length == 0
                          ? SingletonField
                          : from p in props
                            where p.CanRead
                            select p.Name.AsKeyTo(new Func<T, object>(e => p.GetValue(e)))
                      select e;

            return fields.Aggregate(this, (b, e) => b.Field(nameMapper(e.Key), e.Value));
        }

        public IQuery<string> Lines(bool includeHeaders = false) =>
            !HasFields
            ? Reflect().Lines(includeHeaders)
            : from q in Query.Array(
                  includeHeaders
                      ? Query.Singleton(string.Join(",", Fields.Select(f => Csv.EncodeField(f.Key))))
                      : Query<string>.Empty,
                  from e in _query
                  select string.Join(",", Fields.Select(f => Csv.EncodeField(f.Value(e)))))
              from e in q
              select e;
    }

    public static class CsvQueryOutput
    {
        public static IQuery<string> ToCsv<T>(this IQuery<T> query) =>
            new CsvOutputQueryBuilder<T>(query).Lines();

        public static IQuery<ICsvQueryOutputPartial> Partial =
            Query.Singleton(CsvQueryOutputPartial.Singleton);
    }

    public interface ICsvQueryOutputPartial
    {
        CsvOutputQueryBuilder<T> Source<T>(IQuery<T> query);
    }

    sealed class CsvQueryOutputPartial : ICsvQueryOutputPartial
    {
        internal static readonly ICsvQueryOutputPartial Singleton = new CsvQueryOutputPartial();

        public CsvOutputQueryBuilder<T> Source<T>(IQuery<T> query) => new CsvOutputQueryBuilder<T>(query);

        CsvQueryOutputPartial() { }
    }

    static class Csv
    {
        static readonly char[] UnquotedCsvFieldProhibitedChars = { ',', '"', '\r', '\n' };

        internal static string EncodeField<T>(T value)
        {
            const string quote = "\"";
            const string quotequote = quote + quote;
            var v = string.Format(CultureInfo.InvariantCulture, "{0}", value);
            return v.IndexOfAny(UnquotedCsvFieldProhibitedChars) >= 0
                 ? quote + v.Replace(quote, quotequote) + quote
                 : v;
        }
    }
}

namespace WebLinq.NamingConventions
{
    using System.Text.RegularExpressions;

    public static class SnakeCase
    {
        static string FromPascalCore(string input) =>
            Regex.Replace(input, @"((?<![A-Z]|^)[A-Z]|(?<=[A-Z]+)[A-Z](?=[a-z]))", m => "_" + m.Value);

        public static string FromPascal(string input) =>
            FromPascalCore(input).ToLowerInvariant();

        public static string ScreamingFromPascal(string input) =>
            FromPascalCore(input).ToUpperInvariant();
    }
}