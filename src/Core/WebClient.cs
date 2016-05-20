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
    using System.Net;
    using System.Net.Http;

    public sealed class HttpOptions
    {
        public NameValueCollection Headers { get; set; }
    }

    public interface IWebClient
    {
        HttpResponseMessage Get(Uri url, HttpOptions options);
    }

    public class WebClient : IWebClient
    {
        readonly QueryContext _context;

        public WebClient(QueryContext context)
        {
            _context = context;
        }

        public HttpResponseMessage Get(Uri url, HttpOptions options)
        {
            var http = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            var headers = request.Headers;
            foreach (var e in from hs in new[] { options?.Headers }
                              where hs != null
                              from i in Enumerable.Range(0, hs.Count)
                              select new KeyValuePair<string, string[]>(hs.GetKey(i),
                                                                        hs.GetValues(i)))
            {
                if (e.Value == null)
                    headers.Add(e.Key, e.Value);
                else
                    headers.Remove(e.Key);
            }

            return http.SendAsync(request).Result;
        }
    }
}