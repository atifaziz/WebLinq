#region Copyright (c) 2023 Atif Aziz. All rights reserved.
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

namespace WebLinq;

using System;
using System.Threading;

public sealed class HttpQueryContext : IDisposable
{
    int _id;

    public static HttpQueryContext CreateDefault()
    {
        var httpClient = new SocketsHandlerBasedHttpClient();
        try
        {
            return new HttpQueryContext(httpClient);
        }
        catch
        {
            httpClient.Dispose();
            throw;
        }
    }

    public HttpQueryContext(IHttpClient httpClient) :
        this(httpClient, HttpConfig.Default) { }

    public HttpQueryContext(IHttpClient httpClient, HttpConfig config)
    {
        HttpClient = httpClient;
        Config = config;
    }

    public int NextId() => Interlocked.Increment(ref _id);
    public IHttpClient HttpClient { get; }
    public HttpConfig Config { get; set; }

    public void Dispose() => HttpClient.Dispose();
}
