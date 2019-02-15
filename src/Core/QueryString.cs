// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;
using StringValues = WebLinq.Collections.Strings;

// Source:
// https://github.com/aspnet/AspNetCore/blob/574be0d22c1678ed5f6db990aec78b4db587b267/src/Http/Http.Abstractions/src/QueryString.cs
//
// This is a slightly modified version from the snapshot above with the
// following changes:
//
// - Moved from namespace Microsoft.Extensions.Primitives to one belonging
//   to this project.
// - Renamed from StringValues to Strings.
// - Re-styled to use project conventions.

namespace WebLinq
{
    /// <summary>
    /// Provides correct handling for QueryString value when needed to
    /// reconstruct a request or redirect URI string
    /// </summary>

    public readonly struct QueryString : IEquatable<QueryString>
    {
        readonly string _value;

        /// <summary>
        /// Represents the empty query string. This field is read-only.
        /// </summary>

        public static readonly QueryString Empty = new QueryString(string.Empty);

        /// <summary>
        /// Initialize the query string with a given value. This value must be
        /// in escaped and delimited format with a leading '?' character.
        /// </summary>
        /// <param name="value">
        /// The query string to be assigned to the <see cref="Value"/> property.
        /// </param>

        public QueryString(string value) =>
            _value = !string.IsNullOrEmpty(value) && value[0] != '?'
                   ? throw new ArgumentException("The leading '?' must be included for a non-empty query.", nameof(value))
                   : value;

        /// <summary>
        /// The escaped query string with the leading '?' character
        /// </summary>

        public string Value => _value;

        /// <summary>
        /// True if the query string is not empty
        /// </summary>

        public bool HasValue => !string.IsNullOrEmpty(_value);

        /// <summary>
        /// Provides the query string escaped in a way which is correct for
        /// combining into the URI representation.  A leading '?' character
        /// will be included unless the <see cref="Value"/> is <c>null</c> or
        /// empty. Characters which are potentially dangerous are escaped.
        /// </summary>
        /// <returns>The query string value</returns>

        public override string ToString() => ToUriComponent();

        /// <summary>
        /// Provides the query string escaped in a way which is correct for
        /// combining into the URI representation. A leading '?' character will
        /// be included unless the <see cref="Value"/> is <c>null</c> or empty.
        /// Characters which are potentially dangerous are escaped.
        /// </summary>
        /// <returns>The query string value</returns>

        public string ToUriComponent() =>
            // Escape things properly so System.Uri doesn't mis-interpret the data.
            HasValue ? _value.Replace("#", "%23") : string.Empty;

        /// <summary>
        /// Returns an <see cref="QueryString"/> given the query as it is
        /// escaped in the URI format. The string MUST NOT contain any value
        /// that is not a query.
        /// </summary>
        /// <param name="uriComponent">
        /// The escaped query as it appears in the URI format.</param>
        /// <returns>The resulting <see cref="QueryString"/></returns>

        public static QueryString FromUriComponent(string uriComponent)
            => string.IsNullOrEmpty(uriComponent)
             ? new QueryString(string.Empty)
             : new QueryString(uriComponent);

        /// <summary>
        /// Returns an <see cref="QueryString"/> given the query as from a
        /// <see cref="Uri"/> object. Relative <see cref="Uri"/> objects are not
        /// supported.
        /// </summary>
        /// <param name="uri">The <see cref="Uri"/> object</param>
        /// <returns>The resulting <see cref="QueryString"/></returns>

        public static QueryString FromUriComponent(Uri uri)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            var queryValue = uri.GetComponents(UriComponents.Query, UriFormat.UriEscaped);
            if (!string.IsNullOrEmpty(queryValue))
            {
                queryValue = "?" + queryValue;
            }
            return new QueryString(queryValue);
        }

        /// <summary>
        /// Create a query string with a single given parameter name and value.
        /// </summary>
        /// <param name="name">The un-encoded parameter name</param>
        /// <param name="value">The un-encoded parameter value</param>
        /// <returns>The resulting <see cref="QueryString"/></returns>

        public static QueryString Create(string name, string value)
            => name == null
             ? throw new ArgumentNullException(nameof(name))
             : new QueryString("?" + Encode(name)
                                   + "="
                                   + (!string.IsNullOrEmpty(value) ? Encode(value) : value));

        /// <summary>
        /// Creates a query string composed from the given name value pairs.
        /// </summary>
        /// <returns>The resulting <see cref="QueryString"/></returns>

        public static QueryString Create(IEnumerable<KeyValuePair<string, string>> parameters)
        {
            var builder = new StringBuilder();
            var first = true;
            foreach (var pair in parameters)
            {
                AppendKeyValuePair(builder, pair.Key, pair.Value, first);
                first = false;
            }

            return new QueryString(builder.ToString());
        }

        /// <summary>
        /// Creates a query string composed from the given name value pairs.
        /// </summary>
        /// <returns>The resulting <see cref="QueryString"/></returns>

        public static QueryString Create(IEnumerable<KeyValuePair<string, StringValues>> parameters)
        {
            var builder = new StringBuilder();
            var first = true;

            foreach (var pair in parameters)
            {
                // If nothing in this pair.Values, append null value and continue
                if (StringValues.IsNullOrEmpty(pair.Value))
                {
                    AppendKeyValuePair(builder, pair.Key, null, first);
                    first = false;
                    continue;
                }
                // Otherwise, loop through values in pair.Value
                foreach (var value in pair.Value)
                {
                    AppendKeyValuePair(builder, pair.Key, value, first);
                    first = false;
                }
            }

            return new QueryString(builder.ToString());
        }

        public QueryString Add(QueryString other)
            => !HasValue || Value.Equals("?", StringComparison.Ordinal) ? other
             : !other.HasValue || other.Value.Equals("?", StringComparison.Ordinal) ? this
               // ?name1=value1 Add ?name2=value2 returns ?name1=value1&name2=value2
             : new QueryString(_value + "&" + other.Value.Substring(1));

        public QueryString Add(string name, string value)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            if (!HasValue || Value.Equals("?", StringComparison.Ordinal))
                return Create(name, value);

            var builder = new StringBuilder(Value);
            AppendKeyValuePair(builder, name, value, first: false);
            return new QueryString(builder.ToString());
        }

        public bool Equals(QueryString other)
            => !HasValue && !other.HasValue
            || string.Equals(_value, other._value, StringComparison.Ordinal);

        public override bool Equals(object obj)
            => ReferenceEquals(null, obj)
             ? !HasValue
             : obj is QueryString qs && Equals(qs);

        public override int GetHashCode() =>
            HasValue ? _value.GetHashCode() : 0;

        public static bool operator ==(QueryString left, QueryString right) =>
            left.Equals(right);

        public static bool operator !=(QueryString left, QueryString right) =>
            !left.Equals(right);

        public static QueryString operator +(QueryString left, QueryString right) =>
            left.Add(right);

        static void AppendKeyValuePair(StringBuilder builder, string key, string value, bool first)
        {
            builder.Append(first ? "?" : "&");
            builder.Append(Encode(key));
            builder.Append("=");
            if (!string.IsNullOrEmpty(value))
                builder.Append(Encode(value));
        }

        static string Encode(string value) =>
            UrlEncoder.Default.Encode(value);
    }
}
