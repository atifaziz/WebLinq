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

namespace WebLinq.Html
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Mannex.Collections.Generic;

    /// <summary>
    /// Represents a <a href="https://www.w3.org/TR/html5/forms.html#attr-input-type">control
    /// type that can be specified with the HTML INPUT tag</a>.
    /// </summary>

    public enum KnownHtmlInputType
    {
        Other,
        // https://www.w3.org/TR/html5/forms.html#attr-input-type
        Hidden  , // Hidden             An arbitrary string
        Text    , // Text               Text with no line breaks
        // TODO Search  , // Search             Text with no line breaks
        Tel     , // Telephone          Text with no line breaks
        // TODO Url     , // URL                An absolute URL
        Email   , // E-mail             An e-mail address or list of e-mail addresses
        Password, // Password           Text with no line breaks (sensitive information)
        Date    , // Date               A date (year, month, day) with no time zone
        Time    , // Time               A time (hour, minute, seconds, fractional seconds) with no time zone
        Number  , // Number             A numerical value
        // TODO Range   , // Range              A numerical value, with the extra semantic that the exact value is not important
        // TODO Color   , // Color              An sRGB color with 8-bit red, green, and blue components
        Checkbox, // Checkbox           A set of zero or more values from a predefined list
        Radio   , // Radio Button       An enumerated value
        File    , // File Upload        Zero or more files each with a MIME type and optionally a file name
        Submit  , // Submit Button      An enumerated value, with the extra semantic that it must be the last value selected and initiates form submission
        Image   , // Image Button       A coordinate, relative to a particular image's size, with the extra semantic that it must be the last value selected and initiates form submission
        Reset   , // Reset Button       n/a
        Button  , // Button             n/a
    }

    public sealed class HtmlInputType : IEquatable<HtmlInputType>
    {
        readonly string _type;

        public KnownHtmlInputType KnownType { get; }

        public static readonly HtmlInputType Text     = new HtmlInputType(KnownHtmlInputType.Text    , "text");
        public static readonly HtmlInputType Email    = new HtmlInputType(KnownHtmlInputType.Email   , "email");
        public static readonly HtmlInputType Tel      = new HtmlInputType(KnownHtmlInputType.Tel     , "tel");
        public static readonly HtmlInputType Number   = new HtmlInputType(KnownHtmlInputType.Number  , "number");
        public static readonly HtmlInputType Date     = new HtmlInputType(KnownHtmlInputType.Date    , "date");
        public static readonly HtmlInputType Time     = new HtmlInputType(KnownHtmlInputType.Time    , "time");
        public static readonly HtmlInputType Password = new HtmlInputType(KnownHtmlInputType.Password, "password");
        public static readonly HtmlInputType Checkbox = new HtmlInputType(KnownHtmlInputType.Checkbox, "checkbox");
        public static readonly HtmlInputType Radio    = new HtmlInputType(KnownHtmlInputType.Radio   , "radio");
        public static readonly HtmlInputType Submit   = new HtmlInputType(KnownHtmlInputType.Submit  , "submit");
        public static readonly HtmlInputType Reset    = new HtmlInputType(KnownHtmlInputType.Reset   , "reset");
        public static readonly HtmlInputType File     = new HtmlInputType(KnownHtmlInputType.File    , "file");
        public static readonly HtmlInputType Hidden   = new HtmlInputType(KnownHtmlInputType.Hidden  , "hidden");
        public static readonly HtmlInputType Image    = new HtmlInputType(KnownHtmlInputType.Image   , "image");
        public static readonly HtmlInputType Button   = new HtmlInputType(KnownHtmlInputType.Button  , "button");

        public static HtmlInputType Default => Text;

        static Dictionary<string, HtmlInputType> _lookup;
        static Dictionary<string, HtmlInputType> Lookup => _lookup ?? (_lookup = CreateLookup());

        static Dictionary<string, HtmlInputType> CreateLookup()
        {
            var types =
                from f in typeof(HtmlInputType).GetFields(BindingFlags.Public | BindingFlags.Static)
                where f.FieldType == typeof(HtmlInputType)
                select (HtmlInputType) f.GetValue(null);

            return types.ToDictionary(e => e.ToString(), e => e, StringComparer.OrdinalIgnoreCase);
        }

        public static HtmlInputType Parse(string input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.Length == 0) throw new ArgumentException(null, nameof(input));

            var type = Lookup.Find(input);
            return type == null ? new HtmlInputType(KnownHtmlInputType.Other, input)
                 : type.ToString() == input ? type
                 : new HtmlInputType(type.KnownType, input);
        }

        public HtmlInputType(KnownHtmlInputType knowType, string type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (type.Length == 0) throw new ArgumentException(null, nameof(type));

            // ReSharper disable once ForCanBeConvertedToForeach
            // ReSharper disable once LoopCanBeConvertedToQuery
            for (var i = 0; i < type.Length; i++)
            {
                var ch = type[i] & ~0x20;
                if ((ch < '0' || ch > '9') && (ch < 'A' || ch > 'Z'))
                    throw new ArgumentException(null, nameof(type));
            }

            _type = type;
            KnownType = knowType;
        }

        public bool Equals(HtmlInputType other) =>
            other != null
            && ((other.KnownType == KnownHtmlInputType.Other
                    && string.Equals(_type, other._type, StringComparison.OrdinalIgnoreCase))
                || KnownType == other.KnownType);

        public override bool Equals(object obj) => Equals(obj as HtmlInputType);

        public override int GetHashCode() =>
            KnownType == KnownHtmlInputType.Other
            ? StringComparer.OrdinalIgnoreCase.GetHashCode(_type) * 397
            : (int) KnownType;

        public override string ToString() => _type;

        public static bool operator ==(HtmlInputType a, HtmlInputType b) =>
            !ReferenceEquals(a, null) ? a.Equals(b) : ReferenceEquals(b, null);

        public static bool operator !=(HtmlInputType a, HtmlInputType b) => !(a == b);
    }
}