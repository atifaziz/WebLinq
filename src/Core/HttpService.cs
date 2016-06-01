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
    using System.Collections.Specialized;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using Mannex.Collections.Generic;

    public sealed class HttpOptions
    {
        public HttpHeaderCollection Headers { get; set; }
    }

    public abstract class HttpService
    {
        protected HttpService() : this(0) {}
        protected HttpService(int fetchId) { FetchId = fetchId; }

        public int FetchId { get; private set; }
        protected int NextFetchId() => ++FetchId;

        public abstract HttpFetch<HttpContent> Get(Uri url, HttpOptions options);
        public abstract HttpFetch<HttpContent> Post(Uri url, HttpContent content, HttpOptions options);
    }

    public class SysNetHttpService : HttpService
    {
        public HttpClient HttpClient { get; }

        public SysNetHttpService() : this(null) {}

        public SysNetHttpService(HttpClient client)
        {
            HttpClient = client ?? new HttpClient();
        }

        public void Register(Action<Type, object> registrationHandler) =>
            registrationHandler(typeof(HttpService), this);

        public override HttpFetch<HttpContent> Get(Uri url, HttpOptions options)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            MergeHeaders(options?.Headers, request.Headers);
            return HttpClient.SendAsync(request).Result.ToHttpFetch(NextFetchId());
        }

        public override HttpFetch<HttpContent> Post(Uri url, HttpContent content, HttpOptions options)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };
            MergeHeaders(options?.Headers, request.Headers);
            return HttpClient.SendAsync(request).Result.ToHttpFetch(NextFetchId());
        }

        static void MergeHeaders(NameValueCollection source, HttpHeaders target)
        {
            if (source == null)
                return;

            foreach (var e in from i in Enumerable.Range(0, source.Count)
                              select source.GetKey(i).AsKeyTo(source.GetValues(i)))
            {
                if (e.Value == null)
                    target.Remove(e.Key);
                else
                    target.Add(e.Key, e.Value);
            }
        }
    }
}