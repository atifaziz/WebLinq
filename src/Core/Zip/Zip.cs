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
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using Mannex.IO;
    using Mime;

    #endregion

    public sealed class ZipEntry
    {
        string _fileName;
        string _dirPath;

        public string Name          { get; }
        public string DirPath       => DirPathFileNameFault._dirPath;
        public string FileName      => DirPathFileNameFault._fileName;
        public long Length          { get; }
        public DateTimeOffset Time  { get; }

        public ZipEntry(string name, long length, DateTimeOffset time)
        {
            Name   = name;
            Length = length;
            Time   = time;
        }

        ZipEntry DirPathFileNameFault
        {
            get
            {
                if (_fileName == null)
                    SplitDirPathFileName(Name, out _dirPath, out _fileName);
                return this;
            }
        }

        static readonly char[] PathSeparators = { '/', '\\' };

        static void SplitDirPathFileName(string name, out string dirPath, out string fileName)
        {
            var i = name.LastIndexOfAny(PathSeparators);
            if (i < 0)
            {
                dirPath = string.Empty;
                fileName = name;
            }
            else
            {
                dirPath  = name.Substring(0, i + 1);
                fileName = name.Substring(i + 1);
            }
        }

        public override string ToString() => Name;
    }

    public sealed class Zip
    {
        readonly string _path;

        public Zip(string path) { _path = path; }

        public IEnumerable<ZipEntry> GetEntries() =>
            GetEntries((name, length, _, time) => new ZipEntry(name, length, time));

        public IEnumerable<T> GetEntries<T>(Func<string, long, long, DateTimeOffset, T> selector)
        {
            using (var zip = Open())
            foreach (var e in zip.Entries)
                yield return selector(e.FullName, e.Length, e.CompressedLength, e.LastWriteTime);
        }

        public IEnumerable<T> Extract<T>(Func<ZipEntry, Stream, T> extractor) =>
            Extract(null, extractor);

        public IEnumerable<byte[]> Extract(Func<ZipEntry, bool> predicate) =>
            Extract(predicate, ZipExtractors.Buffer);

        public IEnumerable<T> Extract<T>(Func<ZipEntry, bool> predicate,
                                         Func<ZipEntry, Stream, T> extractor)
        {
            using (var zip = Open())
            foreach (var e in from e in zip.Entries
                              let ext = new ZipEntry(e.FullName, e.Length, e.LastWriteTime)
                              where predicate?.Invoke(ext) ?? true
                              select new { Int = e, Ext = ext })
            {
                using (var s = e.Int.Open())
                    yield return extractor(e.Ext, s);
            }
        }

        ZipArchive Open()
        {
            var input = File.OpenRead(_path);
            try
            {
                var zip = new ZipArchive(input, ZipArchiveMode.Read, leaveOpen: false);
                input = null; // ownership transferred
                return zip;
            }
            finally
            {
                input?.Dispose();
            }
        }

        public override string ToString() => _path;

    }

    public sealed class ZipExtractors
    {
        public static string Utf8Text(ZipEntry entry, Stream input) =>
            Text(input, Encoding.UTF8);

        public static Func<ZipEntry, Stream, string> Text(Encoding encoding) =>
            (_, input) => Text(input, encoding);

        static string Text(Stream input, Encoding encoding)
        {
            using (var reader = new StreamReader(input, encoding))
                return reader.ReadToEnd();
        }

        public static HttpContent HttpContent(ZipEntry entry, Stream input) =>
            HttpContent(entry, input, null);

        public static Func<ZipEntry, Stream, HttpContent> HttpContent(Func<string, string> mimeMapper) =>
            (e, input) => HttpContent(e, input, mimeMapper);

        static HttpContent HttpContent(ZipEntry entry, Stream input, Func<string, string> mimeMapper)
        {
            if (entry.FileName.Length == 0) throw new ArgumentException("ZIP entry must be a file.", nameof(entry));

            var content = new StreamContent(input);
            var headers = content.Headers;
            headers.ContentLength      = entry.Length;
            headers.ContentType        = new MediaTypeHeaderValue((mimeMapper ?? MimeMapping.FindMimeTypeFromFileName)(entry.FileName));
            headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName         = entry.FileName,
                ModificationDate = entry.Time
            };
            return content;
        }

        public static Stream Memorize(ZipEntry entry, Stream input) =>
            input.Memorize();

        public static byte[] Buffer(ZipEntry entry, Stream input)
        {
            if (entry.Length > int.MaxValue)
                throw new Exception($"Uncompressed stream is too large ({entry.Length:N0} bytes) to buffer.");

            var length = (int) entry.Length;
            var buffer = new byte[length];
            var offset = 0;
            int read;
            do
            {
                read = input.Read(buffer, offset, length);
                offset += read;
                length -= read;
            }
            while (read > 0 && length > 0);
            return buffer;
        }
    }
}
