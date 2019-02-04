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

    [DebuggerDisplay("Id = {Id}, StatusCode = {StatusCode} ({ReasonPhrase})")]
    public partial class HttpFetch
    {
        public int Id                              { get; }
        public Version HttpVersion                 { get; }
        public HttpStatusCode StatusCode           { get; }
        public string ReasonPhrase                 { get; }
        public HttpHeaderCollection Headers        { get; }
        public HttpHeaderCollection ContentHeaders { get; }
        public Uri RequestUrl                      { get; }
        public HttpHeaderCollection RequestHeaders { get; }

        public HttpFetch(int id,
            Version httpVersion,
            HttpStatusCode statusCode,
            string reasonPhrase,
            HttpHeaderCollection headers,
            HttpHeaderCollection contentHeaders,
            Uri requestUrl,
            HttpHeaderCollection requestHeaders)
        {
            Id             = id;
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
    }

    partial class HttpFetch
    {
        public static HttpFetch<T> Create<T>(int id,
                                             T content,
                                             IHttpClient client,
                                             Version httpVersion,
                                             HttpStatusCode statusCode,
                                             string reasonPhrase,
                                             HttpHeaderCollection headers,
                                             HttpHeaderCollection contentHeaders,
                                             Uri requestUrl,
                                             HttpHeaderCollection requestHeaders) =>
            new HttpFetch<T>(id,
                             httpVersion, statusCode, reasonPhrase,
                             headers, contentHeaders,
                             requestUrl, requestHeaders,
                             client, content);
    }

    [DebuggerDisplay("Id = {Id}, StatusCode = {StatusCode} ({ReasonPhrase}), Content = {Content}")]
    public partial class HttpFetch<T> : HttpFetch
    {
        public IHttpClient Client { get; }
        public T Content          { get; }

        public HttpFetch(int id,
                         Version httpVersion,
                         HttpStatusCode statusCode,
                         string reasonPhrase,
                         HttpHeaderCollection headers,
                         HttpHeaderCollection contentHeaders,
                         Uri requestUrl,
                         HttpHeaderCollection requestHeaders,
                         IHttpClient client, T content) :
            base(id, httpVersion, statusCode, reasonPhrase,
                 headers, contentHeaders,
                 requestUrl, requestHeaders)
        {
            Client = client;
            Content = content;
        }

        HttpFetch(HttpFetch fetch, IHttpClient client, T content) :
            this(fetch.Id,
                 fetch.HttpVersion, fetch.StatusCode, fetch.ReasonPhrase,
                 fetch.Headers, fetch.ContentHeaders,
                 fetch.RequestUrl, fetch.RequestHeaders,
                 client, content) {}

        public HttpFetch<TContent> WithContent<TContent>(TContent content) =>
            new HttpFetch<TContent>(this, Client, content);

        internal HttpFetch<T> WithConfig(HttpConfig config) =>
            new HttpFetch<T>(this, Client.WithConfig(config), Content);
    }

}
