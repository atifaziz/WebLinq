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
                                             IHttpClient client,
                                             Version httpVersion,
                                             HttpStatusCode statusCode,
                                             string reasonPhrase,
                                             HttpHeaderCollection headers,
                                             HttpHeaderCollection contentHeaders,
                                             Uri requestUrl,
                                             HttpHeaderCollection requestHeaders) =>
            new HttpFetch<T>(client,
                             new HttpFetchInfo(id,
                                httpVersion, statusCode, reasonPhrase, headers, contentHeaders,
                                requestUrl, requestHeaders),
                             content);
    }

    [DebuggerDisplay("Id = {Id}, StatusCode = {StatusCode} ({ReasonPhrase}), Content = {Content}")]
    public partial class HttpFetch<T>
    {
        public IHttpClient Client                  { get; }
        public HttpFetchInfo Info                  { get; }
        public T Content                           { get; }

        public int Id                              => Info.Id;
        public Version HttpVersion                 => Info.HttpVersion;
        public HttpStatusCode StatusCode           => Info.StatusCode;
        public string ReasonPhrase                 => Info.ReasonPhrase;
        public HttpHeaderCollection Headers        => Info.Headers;
        public HttpHeaderCollection ContentHeaders => Info.ContentHeaders;
        public Uri RequestUrl                      => Info.RequestUrl;
        public HttpHeaderCollection RequestHeaders => Info.RequestHeaders;

        public HttpFetch(IHttpClient client, HttpFetchInfo info, T content)
        {
            Client  = client;
            Info    = info;
            Content = content;
        }

        public bool IsSuccessStatusCode => Info.IsSuccessStatusCode;
        public bool IsSuccessStatusCodeInRange(int first, int last) => Info.IsSuccessStatusCodeInRange(first, last);

        public HttpFetch<TContent> WithContent<TContent>(TContent content) =>
            new HttpFetch<TContent>(Client, Info, content);

        public HttpFetch<T> WithConfig(HttpConfig config) =>
            new HttpFetch<T>(Client.WithConfig(config), Info, Content);
    }

    [DebuggerDisplay("Id = {Id}, StatusCode = {StatusCode} ({ReasonPhrase})")]
    public sealed class HttpFetchInfo
    {
        public int Id                              { get; }
        public Version HttpVersion                 { get; }
        public HttpStatusCode StatusCode           { get; }
        public string ReasonPhrase                 { get; }
        public HttpHeaderCollection Headers        { get; }
        public HttpHeaderCollection ContentHeaders { get; }
        public Uri RequestUrl                      { get; }
        public HttpHeaderCollection RequestHeaders { get; }

        public HttpFetchInfo(int id,
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
}
