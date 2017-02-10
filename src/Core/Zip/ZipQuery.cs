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
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    public static class ZipQuery
    {
        public static IObservable<HttpFetch<Zip>> DownloadZip(this IHttpObservable query) =>
            from fetch in query.WithReader(async f => await DownloadZip(f.Content))
            select fetch.WithContent(new Zip(fetch.Content));

        public static IObservable<HttpFetch<Zip>> DownloadZip(this IObservable<HttpFetch<HttpContent>> query) =>
            from fetch in query
            from path in DownloadZip(fetch.Content)
            select fetch.WithContent(new Zip(path));

        static async Task<string> DownloadZip(HttpContent content)
        {
            var path = Path.GetTempFileName();
            using (var output = File.Create(path))
                await content.CopyToAsync(output).DontContinueOnCapturedContext();
            return path;
        }
    }
}
