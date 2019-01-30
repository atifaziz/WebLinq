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
    using System.Globalization;

    sealed class UriFormatProvider : IFormatProvider
    {
        public static readonly IFormatProvider InvariantCulture =
            new UriFormatProvider(CultureInfo.InvariantCulture);

        readonly IFormatProvider _baseProvider;

        public UriFormatProvider() :
            this(null) {}

        public UriFormatProvider(IFormatProvider baseProvider) =>
            _baseProvider = baseProvider;

        public object GetFormat(Type formatType)
            => formatType == null ? throw new ArgumentNullException(nameof(formatType))
             : formatType == typeof(ICustomFormatter) ? UriFormatter.Default
             : _baseProvider != null ? _baseProvider.GetFormat(formatType)
             : CultureInfo.CurrentCulture.GetFormat(formatType);

        sealed class UriFormatter : ICustomFormatter
        {
            public static readonly UriFormatter Default = new UriFormatter();

            UriFormatter() {}

            public string Format(string format, object arg, IFormatProvider formatProvider)
                => arg == null ? string.Empty
                 : Uri.EscapeDataString(arg is IFormattable formattable
                                        ? formattable.ToString(format, formatProvider)
                                        : arg.ToString());
        }
    }
}
