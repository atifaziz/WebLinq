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
    using System.Net.Http;
    using System.Xml.Linq;

    public static class XmlQuery
    {
        public static Query<XDocument> Xml(this Query<HttpResponseMessage> query) =>
            Xml(query, LoadOptions.None);

        public static Query<XDocument> Xml(this Query<HttpResponseMessage> query, LoadOptions options) =>
            query.Bind(response => new Query<XDocument>(context => QueryResult.Create(context, System.Xml.Linq.XDocument.Load(response.Content.ReadAsStreamAsync().Result, options))));
    }
}
