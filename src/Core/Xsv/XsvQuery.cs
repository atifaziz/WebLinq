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
    using System;
    using System.Data;
    using System.Net.Http;
    using System.Reactive.Linq;
    using Mannex.Data;
    using Mannex.IO;
    using Text;

    public static class XsvQuery
    {
        public static IObservable<HttpFetch<DataTable>> XsvToDataTable(this IObservable<HttpFetch<HttpContent>> query, string delimiter, bool quoted, params DataColumn[] columns) =>
            from fetch in query.Text()
            select fetch.WithContent(fetch.Content.Read().ParseXsvAsDataTable(delimiter, quoted, columns));

        public static IObservable<DataTable> XsvToDataTable(this IObservable<string> query, string delimiter, bool quoted, params DataColumn[] columns) =>
            from xsv in query
            select xsv.Read().ParseXsvAsDataTable(delimiter, quoted, columns);

        public static IObservable<DataTable> XsvToDataTable(string text, string delimiter, bool quoted, params DataColumn[] columns)
        {
            DataTable dt = null;
            return Observable.Defer(() => Observable.Return(dt ?? (dt = text.Read().ParseXsvAsDataTable(delimiter, quoted, columns))));
        }

        public static IObservable<HttpFetch<DataTable>> CsvToDataTable(this IObservable<HttpFetch<HttpContent>> query, params DataColumn[] columns) =>
            query.XsvToDataTable(",", true, columns);

        public static IObservable<DataTable> CsvToDataTable(this IObservable<string> query, params DataColumn[] columns) =>
            query.XsvToDataTable(",", true, columns);
    }
}
