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

    public static class ParsedValue
    {
        public static ParsedValue<TSource, TValue> Create<TSource, TValue>(TSource source, TValue value) =>
            new ParsedValue<TSource,TValue>(source, value);
    }

    public static class ValueParser
    {
        public static Func<TSource, ParsedValue<TSource, TValue>> Create<TSource, TValue>(Func<TSource, TValue> parser) =>
            s => ParsedValue.Create(s, parser(s));
    }

    public static class ValueTextParser
    {
        public static Func<string, ParsedValue<string, T>> Create<T>(Func<string, T> parser) =>
            ValueParser.Create(parser);

        public static ParsedValue<string, int> ParseInt32(this string source) =>
            ParseInt32(source, null);

        public static ParsedValue<string, int> ParseInt32(this string source, IFormatProvider provider) =>
            ParseInt32(source, NumberStyles.Integer, provider);

        public static ParsedValue<string, int> ParseInt32(this string source, NumberStyles styles) =>
            ParseInt32(source, styles, null);

        public static ParsedValue<string, int> ParseInt32(this string source, NumberStyles styles, IFormatProvider provider) =>
            ParsedValue.Create(source, int.Parse(source, styles, CultureInfo.InvariantCulture));

        public static ParsedValue<string, double> ParseDouble(this string source) =>
            ParseDouble(source, null);

        public static ParsedValue<string, double> ParseDouble(this string source, IFormatProvider provider) =>
            ParseDouble(source, NumberStyles.Float | NumberStyles.AllowThousands, provider);

        public static ParsedValue<string, double> ParseDouble(this string source, NumberStyles styles) =>
            ParseDouble(source, styles, null);

        public static ParsedValue<string, double> ParseDouble(this string source, NumberStyles styles, IFormatProvider provider) =>
            ParsedValue.Create(source, double.Parse(source, styles, provider));

        public static ParsedValue<string, decimal> ParseDecimal(this string source) =>
            ParseDecimal(source, null);

        public static ParsedValue<string, decimal> ParseDecimal(this string source, IFormatProvider provider) =>
            ParseDecimal(source, NumberStyles.Number, provider);

        public static ParsedValue<string, decimal> ParseDecimal(this string source, NumberStyles styles) =>
            ParseDecimal(source, styles, null);

        public static ParsedValue<string, decimal> ParseDecimal(this string source, NumberStyles styles, IFormatProvider provider) =>
            ParsedValue.Create(source, decimal.Parse(source, styles, provider));

        public static ParsedValue<string, DateTime> ParseDateTime(this string source) =>
            ParseDateTime(source, (IFormatProvider) null);

        public static ParsedValue<string, DateTime> ParseDateTime(this string source, IFormatProvider provider) =>
            ParsedValue.Create(source, DateTime.Parse(source, provider));

        public static ParsedValue<string, DateTime> ParseDateTime(this string source, string format) =>
            ParseDateTime(source, format, null);

        public static ParsedValue<string, DateTime> ParseDateTime(this string source, string format, IFormatProvider provider) =>
            ParsedValue.Create(source, DateTime.ParseExact(source, format, provider));
    }

    public struct ParsedValue<TSource, TValue> : IEquatable<ParsedValue<TSource, TValue>>
    {
        public TSource Source { get; }
        public TValue Value { get; }

        public ParsedValue(TSource source, TValue value)
        {
            Source = source;
            Value = value;
        }

        public bool Equals(ParsedValue<TSource, TValue> other) =>
            EqualityComparer<TSource>.Default.Equals(Source, other.Source)
            && EqualityComparer<TValue>.Default.Equals(Value, other.Value);

        public override bool Equals(object obj) =>
            obj is ParsedValue<TSource, TValue> && Equals((ParsedValue<TSource, TValue>) obj);

        public override int GetHashCode() =>
            unchecked((EqualityComparer<TSource>.Default.GetHashCode(Source) * 397)
                      ^ EqualityComparer<TValue>.Default.GetHashCode(Value));

        public override string ToString() =>
            Value == null ? string.Empty : Value.ToString();
    }
}