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

    public static class HttpFetch
    {
        public static HttpFetch<T> Create<T>(int id,
                                             T content,
                                             IHttpClient<HttpConfig> client,
                                             Version httpVersion,
                                             HttpStatusCode statusCode,
                                             string reasonPhrase,
                                             HttpHeaderCollection headers,
                                             Uri requestUrl,
                                             HttpHeaderCollection requestHeaders) =>
            new HttpFetch<T>(id, content, client,
                             httpVersion, statusCode, reasonPhrase, headers,
                             requestUrl, requestHeaders);
    }

    [DebuggerDisplay("Id = {Id}, StatusCode = {StatusCode} ({ReasonPhrase}), Content = {Content}")]
    public partial class HttpFetch<T> : IDisposable
    {
        bool _disposed;

        public int Id                              { get; }
        public T Content                           { get; private set; }
        public IHttpClient<HttpConfig> Client      { get; }
        public Version HttpVersion                 { get; }
        public HttpStatusCode StatusCode           { get; }
        public string ReasonPhrase                 { get; }
        public HttpHeaderCollection Headers        { get; }
        public Uri RequestUrl                      { get; }
        public HttpHeaderCollection RequestHeaders { get; }

        public HttpFetch(int id,
                         T content,
                         IHttpClient<HttpConfig> client,
                         Version httpVersion,
                         HttpStatusCode statusCode,
                         string reasonPhrase,
                         HttpHeaderCollection headers,
                         Uri requestUrl,
                         HttpHeaderCollection requestHeaders)
        {
            Id             = id;
            Content        = content;
            Client         = client;
            HttpVersion    = httpVersion;
            StatusCode     = statusCode;
            ReasonPhrase   = reasonPhrase;
            Headers        = headers;
            RequestUrl     = requestUrl;
            RequestHeaders = requestHeaders;
        }

        public bool IsSuccessStatusCode => IsSuccessStatusCodeInRange(200, 299);
        public bool IsSuccessStatusCodeInRange(int first, int last) => (int)StatusCode >= first && (int)StatusCode <= last;

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            var disposable = Content as IDisposable;
            Content = default(T);
            disposable?.Dispose();
        }

        public HttpFetch<TContent> WithContent<TContent>(TContent content) =>
            new HttpFetch<TContent>(Id, content, Client,
                                    HttpVersion, StatusCode, ReasonPhrase, Headers,
                                    RequestUrl, RequestHeaders);
    }
}
