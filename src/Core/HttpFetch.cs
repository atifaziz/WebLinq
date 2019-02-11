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
    using System.Diagnostics;
    using System.Net;
    using System.Net.Http.Headers;
    using System.Text;

    public static class HttpFetch
    {
        public static HttpFetch<T> Create<T>(HttpFetchInfo info, T content) =>
            new HttpFetch<T>(info, content);
    }

    [DebuggerDisplay("Id = {Id}, StatusCode = {StatusCode} ({ReasonPhrase}), Content = {Content}")]
    public partial class HttpFetch<T>
    {
        public HttpFetchInfo Info                  { get; }
        public T Content                           { get; }

        public int Id                              => Info.Id;
        public IHttpClient Client                  => Info.Client;
        public Version HttpVersion                 => Info.HttpVersion;
        public HttpStatusCode StatusCode           => Info.StatusCode;
        public string ReasonPhrase                 => Info.ReasonPhrase;
        public HttpHeaderCollection Headers        => Info.Headers;
        public HttpHeaderCollection ContentHeaders => Info.ContentHeaders;
        public Uri RequestUrl                      => Info.RequestUrl;
        public HttpHeaderCollection RequestHeaders => Info.RequestHeaders;

        public HttpFetch(HttpFetchInfo info, T content)
        {
            Info    = info;
            Content = content;
        }

        public HttpFetch<TContent> WithContent<TContent>(TContent content) =>
            new HttpFetch<TContent>(Info, content);

        public bool IsSuccessStatusCode => Info.IsSuccessStatusCode;
        public bool IsSuccessStatusCodeInRange(int first, int last) => Info.IsSuccessStatusCodeInRange(first, last);

        public bool IsContentType(string type) => Info.IsContentType(type);
        public string ContentMediaType         => Info.ContentMediaType;
        public string ContentCharSet           => Info.ContentCharSet;
        public Encoding ContentCharSetEncoding => Info.ContentCharSetEncoding;

        public string ContentDispositionType     => Info.ContentDispositionType;
        public string ContentDispositionFileName => Info.ContentDispositionFileName;
    }

    [DebuggerDisplay("Id = {Id}, StatusCode = {StatusCode} ({ReasonPhrase})")]
    public sealed class HttpFetchInfo
    {
        public int Id                              { get; }
        public IHttpClient Client                  { get; }
        public Version HttpVersion                 { get; }
        public HttpStatusCode StatusCode           { get; }
        public string ReasonPhrase                 { get; }
        public HttpHeaderCollection Headers        { get; }
        public HttpHeaderCollection ContentHeaders { get; }
        public Uri RequestUrl                      { get; }
        public HttpHeaderCollection RequestHeaders { get; }

        public HttpFetchInfo(int id,
            IHttpClient client,
            Version httpVersion,
            HttpStatusCode statusCode,
            string reasonPhrase,
            HttpHeaderCollection headers,
            HttpHeaderCollection contentHeaders,
            Uri requestUrl,
            HttpHeaderCollection requestHeaders)
        {
            Id             = id;
            Client         = client;
            HttpVersion    = httpVersion;
            StatusCode     = statusCode;
            ReasonPhrase   = reasonPhrase;
            Headers        = headers;
            ContentHeaders = contentHeaders;
            RequestUrl     = requestUrl;
            RequestHeaders = requestHeaders;
        }

        public bool IsSuccessStatusCode => IsSuccessStatusCodeInRange(200, 299);
        public bool IsSuccessStatusCodeInRange(int first, int last) => (int)StatusCode >= first && (int)StatusCode <= last;

        (bool, MediaTypeHeaderValue Value) _contentType;

        MediaTypeHeaderValue ContentType =>
            this.LazyGet(ref _contentType,
                         it => it.ContentHeaders.TryGetValue("Content-Type", out var value)
                         ? MediaTypeHeaderValue.Parse(value)
                         : null);

        public bool IsContentType(string type) =>
            string.Equals(ContentType?.MediaType, type, StringComparison.OrdinalIgnoreCase);

        public string ContentMediaType => ContentType?.MediaType;
        public string ContentCharSet => ContentType?.CharSet;

        public Encoding ContentCharSetEncoding
            => ContentType?.CharSet is string s
             ? Encoding.GetEncoding(s)
             : null;

        (bool, ContentDispositionHeaderValue Value) _contentDisposition;

        ContentDispositionHeaderValue ContentDisposition =>
            this.LazyGet(ref _contentDisposition,
                         it => it.ContentHeaders.TryGetValue("Content-Disposition", out var value)
                             ? ContentDispositionHeaderValue.Parse(value)
                             : null);

        public string ContentDispositionType => ContentDisposition?.DispositionType;

        static readonly char[] Quote = { '"' };

        public string ContentDispositionFileName
            => ContentDisposition is ContentDispositionHeaderValue h
             ? (h.FileNameStar ?? h.FileName)?.Trim(Quote)
             : null;
    }
}
