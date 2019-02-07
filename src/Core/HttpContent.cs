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
    using System.IO;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;

    public sealed class HttpContent : IDisposable
    {
        System.Net.Http.HttpContent _content;

        internal HttpContent(System.Net.Http.HttpContent content) =>
            _content = content ?? throw new ArgumentNullException(nameof(content));

        System.Net.Http.HttpContent Content =>
            _content ?? throw new ObjectDisposedException(nameof(HttpContent));

        public void Dispose()
        {
            var content = _content;
            _content = null;
            content?.Dispose();
        }

        public Task CopyToAsync(Stream stream) =>
            Content.CopyToAsync(stream);

        public Task LoadIntoBufferAsync() =>
            Content.LoadIntoBufferAsync();

        public Task LoadIntoBufferAsync(long maxBufferSize) =>
            Content.LoadIntoBufferAsync(maxBufferSize);

        public Task<byte[]> ReadAsByteArrayAsync() =>
            Content.ReadAsByteArrayAsync();

        public Task<Stream> ReadAsStreamAsync() =>
            Content.ReadAsStreamAsync();

        public Task<string> ReadAsStringAsync() =>
            Content.ReadAsStringAsync();

        public HttpContentHeaders Headers =>
            Content.Headers;
    }
}
