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

namespace WebLinq.Xml
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Xml.Linq;

    public static class XmlQuery
    {
        public static IEnumerable<XDocument> Xml(HttpContent content) =>
            Xml(content, LoadOptions.None);

        public static IEnumerable<XDocument> Xml(HttpContent content, LoadOptions options)
        {
            yield return Xml(content.ReadAsStringAsync().Result, options);
        }

        public static IEnumerable<HttpFetch<XDocument>> Xml(this IEnumerable<HttpFetch<HttpContent>> query) =>
            Xml(query, LoadOptions.None);

        public static IEnumerable<HttpFetch<XDocument>> Xml(this IEnumerable<HttpFetch<HttpContent>> query, LoadOptions options) =>
            from fetch in query
            select fetch.WithContent(Xml(fetch.Content.ReadAsStringAsync().Result, options));

        static XDocument Xml(string xml, LoadOptions options) =>
            XDocument.Parse(xml, options);
    }
}
