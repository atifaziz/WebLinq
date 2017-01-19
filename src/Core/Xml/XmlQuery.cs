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
    using System;
    using System.Net.Http;
    using System.Reactive.Linq;
    using System.Xml.Linq;

    public static class XmlQuery
    {
        public static IObservable<XDocument> Xml(HttpContent content) =>
            Xml(content, LoadOptions.None);

        public static IObservable<XDocument> Xml(HttpContent content, LoadOptions options) =>
            from c in Observable.Return(content)
            from xml in c.ReadAsStringAsync()
            select XDocument.Parse(xml, options);

        public static IObservable<HttpFetch<XDocument>> Xml(this IObservable<HttpFetch<HttpContent>> query) =>
            query.Xml(LoadOptions.None);

        public static IObservable<HttpFetch<XDocument>> Xml(this IObservable<HttpFetch<HttpContent>> query, LoadOptions options) =>
            from e in query
            from xml in Xml(e.Content, options)
            select e.WithContent(xml);
    }
}
