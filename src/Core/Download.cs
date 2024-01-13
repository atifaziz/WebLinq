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
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using static FileSystem;

    public sealed class LocalFileContent : HttpContent
    {
        public string Path { get; }

        public LocalFileContent(string path) =>
            Path = path ?? throw new ArgumentNullException(nameof(path));

        protected override Task<Stream> CreateContentReadStreamAsync() =>
            Task.FromResult((Stream) File.OpenRead(Path));

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
#pragma warning disable CA2000 // Dispose objects before losing scope
            var fs = File.OpenRead(Path);
#pragma warning restore CA2000 // Dispose objects before losing scope
            await using (fs.ConfigureAwait(false))
                await fs.CopyToAsync(stream).ConfigureAwait(false);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = GetFileSize(Path);
            return true;
        }
    }

    partial class HttpQuery
    {
        public static IHttpQuery<HttpFetch<HttpContent>> AsHttpContent<T>(this IHttpQuery<HttpFetch<T>> query)
            where T : HttpContent =>
            from fetch in query
            select fetch.WithContent<HttpContent>(fetch.Content);
        /*
        public static IHttpQuery<HttpFetch<LocalFileContent>> Download(this IHttpQuery<HttpFetch<HttpContent>> query, string path) =>
            query.SelectMany(async f => f.WithContent(await DownloadAsync(f.Content, path)));
        */
        public static IHttpQuery<HttpFetch<LocalFileContent>> Download(this IHttpQuery query, string path)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (path == null) throw new ArgumentNullException(nameof(path));

            return query.ReadContent(HttpContentReader.Create((_, content, cancellationToken) =>
                                                                  DownloadAsync(content, path, cancellationToken)));
        }

        static async Task<LocalFileContent> DownloadAsync(HttpContent content, string path,
                                                          CancellationToken cancellationToken)
        {
#pragma warning disable CA2000 // Dispose objects before losing scope
            var output = File.OpenWrite(path);
#pragma warning restore CA2000 // Dispose objects before losing scope
            await using (output.ConfigureAwait(false))
            {
                await content.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
                var localContent = new LocalFileContent(path);
                foreach (var e in content.Headers)
                    localContent.Headers.TryAddWithoutValidation(e.Key, e.Value);
                return localContent;
            }
        }

        public static IHttpQuery<HttpFetch<LocalFileContent>> DownloadTemp(this IHttpQuery query) =>
            query.Download(Path.GetTempFileName());

        public static IHttpQuery<HttpFetch<LocalFileContent>> DownloadTemp(this IHttpQuery query, string path)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            return query.ReadContent(DownloadTemp(path));
        }

        public static IHttpContentReader<LocalFileContent> DownloadTemp(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            return HttpContentReader.Create((_, content, cancellationToken) =>
                                                DownloadTemp(content, path, cancellationToken));
        }

        static async Task<LocalFileContent> DownloadTemp(HttpContent content, string path,
                                                         CancellationToken cancellationToken)
        {
            string tempPath;

            var startTime = DateTime.Now;
            var exceptions = new List<Exception>();

            while (true)
            {
                // NOTE! `Path.GetDirectoryName` returns null for "/" or "\\" but
                // `Path.IsPathRooted` returns true, so we use path verbatim when it is just the
                // root.

                var desiredDir = Path.GetDirectoryName(path);
                var dir = desiredDir is { Length: > 0 } dn ? dn
                        : desiredDir == null && Path.IsPathRooted(path) ? path
                        : Path.GetTempPath();

                var fileName = Path.GetFileNameWithoutExtension(path);
                using var currentProcess = Process.GetCurrentProcess();
                var discriminator = (currentProcess.Id + Environment.TickCount).ToString("x16", CultureInfo.InvariantCulture);
                var tempName = $"{(string.IsNullOrEmpty(fileName) ? "tmp" : fileName)}-{discriminator}{Path.GetExtension(path)}";
                var tempTestPath = Path.Combine(dir, tempName);

                try
                {
                    new FileStream(tempTestPath, FileMode.CreateNew).Close();
                    tempPath = tempTestPath;
                    break;
                }
                catch (IOException e)
                {
                    exceptions.Add(e);

                    // There is no good cross-platform way to know if "IOException" is due to a file
                    // already existing or not so we assume any "IOException" is due to a file
                    // already existing. However, if we've been looping for 5 seconds then something
                    // is seriously wrong and we throw thereafter.

                    if (DateTime.Now - startTime > TimeSpan.FromSeconds(5))
                        throw new IOException(e.Message, new AggregateException(exceptions));
                }

                await Task.Delay(TimeSpan.FromSeconds(0.1), cancellationToken).ConfigureAwait(false);
            }

            return await DownloadAsync(content, tempPath, cancellationToken).ConfigureAwait(false);
        }
    }
}
