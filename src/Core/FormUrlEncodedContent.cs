#region Copyright (c) .NET Foundation and Contributors. All rights reserved.
//
//
// The MIT License (MIT)
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
// https://github.com/dotnet/corefx/blob/e0ba7aa8026280ee3571179cc06431baf1dfaaac/LICENSE.TXT
//
#endregion

namespace WebLinq
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Net.Http.Headers;
    using System.Net;
    using System.Net.Http;

    // Inspiration & credit:
    // https://github.com/dotnet/corefx/blob/e0ba7aa8026280ee3571179cc06431baf1dfaaac/src/System.Net.Http/src/System/Net/Http/FormUrlEncodedContent.cs
    //
    //
    // - System.System.Net.Http.FormUrlEncodedContent(...) can't handle very long parameters/values
    //   https://github.com/dotnet/corefx/issues/1936

    sealed class FormUrlEncodedContent : ByteArrayContent
    {
        public FormUrlEncodedContent(IEnumerable<KeyValuePair<string, string>> nameValueCollection) :
            base(GetContentByteArray(nameValueCollection)) =>
            Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        static readonly Encoding DefaultHttpEncoding = Encoding.GetEncoding("iso-8859-1");

        static byte[] GetContentByteArray(IEnumerable<KeyValuePair<string, string>> nameValueCollection)
        {
            if (nameValueCollection == null)
                throw new ArgumentNullException(nameof(nameValueCollection));

            // Encode and concatenate data
            var builder = new StringBuilder();
            foreach (var pair in nameValueCollection)
            {
                if (builder.Length > 0)
                    builder.Append('&');

                builder.Append(Encode(pair.Key));
                builder.Append('=');
                builder.Append(Encode(pair.Value));
            }

            return DefaultHttpEncoding.GetBytes(builder.ToString());
        }

        static string Encode(string data) =>
            string.IsNullOrEmpty(data)
            ? string.Empty
            : WebUtility.UrlEncode(data).Replace("%20", "+"); // Escape spaces as '+'.
    }
}
