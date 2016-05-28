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
    using System.Collections.Specialized;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using Mannex.Collections.Generic;

    public sealed class HttpOptions
    {
        public int FetchId { get; set; }
        public NameValueCollection Headers { get; set; }
    }

    public interface IWebClient
    {
        HttpFetch<HttpContent> Get(Uri url, HttpOptions options);
        HttpFetch<HttpContent> Post(Uri url, NameValueCollection data, HttpOptions options);
    }

    public class WebClient : IWebClient
    {
        public HttpClient HttpClient { get; }

        public WebClient() : this(null) {}

        public WebClient(HttpClient client)
        {
            HttpClient = client ?? new HttpClient();
        }

        public void Register(Action<Type, object> registrationHandler) =>
            registrationHandler(typeof(IWebClient), this);

        public HttpFetch<HttpContent> Get(Uri url, HttpOptions options)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            MergeHeaders(options?.Headers, request.Headers);
            return HttpClient.SendAsync(request).Result.ToHttpFetch((options?.FetchId).GetValueOrDefault());
        }

        public HttpFetch<HttpContent> Post(Uri url, NameValueCollection data, HttpOptions options)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            MergeHeaders(options?.Headers, request.Headers);

            request.Content = new FormUrlEncodedContent(
                from i in Enumerable.Range(0, data.Count)
                from v in data.GetValues(i)
                select new KeyValuePair<string, string>(data.GetKey(i), v));

            return HttpClient.SendAsync(request).Result.ToHttpFetch((options?.FetchId).GetValueOrDefault());
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