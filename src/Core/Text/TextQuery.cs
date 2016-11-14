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

namespace WebLinq.Text
{
    using System.Net.Http;
    using System.Text;

    public static class TextQuery
    {
        public static IEnumerable<QueryContext, string> Delimited<T>(this IEnumerable<QueryContext, T> query, string delimiter) =>
            query.Aggregate(new StringBuilder(), (sb, e) => sb.Append(e), sb => sb.ToString());


        public static IEnumerable<QueryContext, HttpFetch<string>> Text(this IEnumerable<QueryContext, HttpFetch<HttpContent>> query) =>
            from fetch in query select fetch.WithContent(fetch.Content.ReadAsStringAsync().Result);
    }
}
