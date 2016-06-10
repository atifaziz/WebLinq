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
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Runtime.CompilerServices;

    static class HttpContentExtensions
    {
        // ReSharper disable once ClassNeverInstantiated.Local

        sealed class Properties
        {
            public string ContentType;
        }

        static readonly ConditionalWeakTable<HttpContent, Properties> Table = new ConditionalWeakTable<HttpContent, Properties>();

        static Properties GetProperties(this HttpContent content) =>
            Table.GetOrCreateValue(content);

        internal static string ContentTypeOverride(this HttpContent content) =>
            content.GetProperties().ContentType;

        internal static void ContentTypeOverride(this HttpContent content, string value) =>
            content.GetProperties().ContentType = value;

        public static MediaTypeHeaderValue GetContentType(this HttpContent content)
        {
            var contentType = content.Headers.ContentType;
            var @override = content.ContentTypeOverride();
            if (@override != null)
            {
                return new MediaTypeHeaderValue(@override)
                {
                    CharSet = contentType.CharSet
                };
            }
            return contentType;
        }
    }
}