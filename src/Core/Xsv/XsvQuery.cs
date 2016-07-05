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

namespace WebLinq.Xsv
{
    using System.Data;
    using System.Net.Http;
    using Mannex.Data;
    using Mannex.IO;
    using Text;

    public static class XsvQuery
    {
        public static Query<HttpFetch<DataTable>> XsvToDataTable(this Query<HttpFetch<HttpContent>> query, string delimiter, bool quoted, params DataColumn[] columns) =>
            query.Text().Select(fetch => fetch.WithContent(fetch.Content.Read().ParseXsvAsDataTable(delimiter, quoted, columns)));

        public static Query<DataTable> XsvToDataTable(string text, string delimiter, bool quoted, params DataColumn[] columns) =>
            Query.Create(context =>
                QueryResult.Create(context, text.Read().ParseXsvAsDataTable(delimiter, quoted, columns)));

    }
}
