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

namespace WebLinq.Zip
{
    using System.IO;
    using System.Net.Http;

    public static class ZipQuery
    {
        public static IEnumerable<QueryContext, HttpFetch<Zip>> DownloadZip(this IEnumerable<QueryContext, HttpFetch<HttpContent>> query) =>
            from fetch in query
            select fetch.WithContent(new Zip(DownloadZip(fetch.Content)));

        static string DownloadZip(HttpContent content)
        {
            var path = Path.GetTempFileName();
            using (var output = File.Create(path))
                content.CopyToAsync(output).Wait();
            return path;
        }
    }
}
