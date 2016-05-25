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

    public sealed class HttpOptions
    {
        public NameValueCollection Headers { get; set; }
    }

    public interface IWebClient
    {
        HttpResponseMessage Get(Uri url, HttpOptions options);
        HttpResponseMessage Post(Uri url, NameValueCollection data, HttpOptions options);
    }

    public class WebClient : IWebClient
    {
        public HttpClient HttpClient { get; }

        public WebClient() : this(null) {}

        public WebClient(HttpClient client)
        {
            HttpClient = client ?? new HttpClient();
        }

        public HttpResponseMessage Get(Uri url, HttpOptions options)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            var headers = request.Headers;
            foreach (var e in from hs in new[] { options?.Headers }
                                where hs != null
                                from i in Enumerable.Range(0, hs.Count)
                                select new KeyValuePair<string, string[]>(hs.GetKey(i),
                                                                        hs.GetValues(i)))
            {
                if (e.Value == null)
                    headers.Remove(e.Key);
                else
                    headers.Add(e.Key, e.Value);
            }

            return HttpClient.SendAsync(request).Result;
        }

        public HttpResponseMessage Post(Uri url, NameValueCollection data, HttpOptions options)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);

            var headers = request.Headers;
            GetValue(options?.Headers, headers);

            request.Content = new FormUrlEncodedContent(
                from i in Enumerable.Range(0, data.Count)
                from v in data.GetValues(i)
                select new KeyValuePair<string, string>(data.GetKey(i), v));

            return HttpClient.SendAsync(request).Result;
        }

        static void GetValue(NameValueCollection source, HttpRequestHeaders target)
        {
            if (source == null)
                return;

            var headers =
                from i in Enumerable.Range(0, source.Count)
                select new KeyValuePair<string, string[]>(source.GetKey(i),
                source.GetValues(i));

            foreach (var e in headers)
            {
                if (e.Value == null)
                    target.Add(e.Key, e.Value);
                else
                    target.Remove(e.Key);
            }
        }
    }
}