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
    using System.Net.Http;
    using System.Reactive.Linq;

    public static class ZipQuery
    {
        [Obsolete("Use " + nameof(HttpObservable.Download)
                         + " or " + nameof(HttpObservable.DownloadTemp)
                         + " with " + nameof(AsZip) + " instead.")]
        public static IObservable<HttpFetch<Zip>> DownloadZip(this IHttpObservable query) =>
            query.DownloadTemp("zip").AsZip();

        [Obsolete("Use " + nameof(HttpObservable.Download)
                         + " or " + nameof(HttpObservable.DownloadTemp)
                         + " with " + nameof(AsZip) + " instead.")]
        public static IObservable<HttpFetch<Zip>> DownloadZip(this IObservable<HttpFetch<HttpContent>> query) =>
            from fetch in query.DownloadTemp("zip")
            select fetch.WithContent(new Zip(fetch.Content.Path));

        public static IObservable<HttpFetch<Zip>> AsZip(this IObservable<HttpFetch<LocalFileContent>> query) =>
            from fetch in query
            select fetch.WithContent(new Zip(fetch.Content.Path));
    }
}
