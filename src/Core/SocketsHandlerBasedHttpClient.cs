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
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public sealed class SocketsHandlerBasedHttpClient : IHttpClient
{
    HandlerConfig _config = new();
    HttpClient? _client;
    CookieContainer? _cookies;

    sealed record HandlerConfig
    {
        public TimeSpan Timeout { get; init; }
        public bool UseDefaultCredentials { get; init; }
        public ICredentials? Credentials { get; init; }
        public DecompressionMethods AutomaticDecompression { get; init; }
        public bool IgnoreInvalidServerCertificate { get; init; }
        public Uri? ProxyUrl { get; init; }
    }

    public CookieContainer? GetCookieContainer() => _cookies;

    public async Task<HttpResponseMessage> SendAsync(HttpConfig config,
                                                     HttpRequestMessage request,
                                                     HttpCompletionOption completionOption,
                                                     CancellationToken cancellationToken)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));

        if (config.IgnoreInvalidServerCertificate)
            throw new NotSupportedException($"{nameof(HttpConfig)}.{nameof(HttpConfig.IgnoreInvalidServerCertificate)} is not supported.");

        var handlerConfig = new HandlerConfig
        {
            Timeout = config.Timeout,
            UseDefaultCredentials = config.UseDefaultCredentials,
            Credentials = config.Credentials,
            AutomaticDecompression = config.AutomaticDecompression,
            IgnoreInvalidServerCertificate = config.IgnoreInvalidServerCertificate,
            ProxyUrl = config.ProxyUrl,
        };

        if (_client is null || handlerConfig != _config)
        {
            _client?.Dispose();

#pragma warning disable CA2000 // Dispose objects before losing scope (eventually owned by client)
            var handler = new SocketsHttpHandler();
#pragma warning restore CA2000 // Dispose objects before losing scope

            try
            {
                handler.AllowAutoRedirect = true;
                handler.UseCookies = true;
                handler.CookieContainer = _cookies;
                handler.Credentials = config.UseDefaultCredentials
                                    ? CredentialCache.DefaultCredentials
                                    : config.Credentials;
                handler.AutomaticDecompression = config.AutomaticDecompression;

                if (config.ProxyUrl is { } proxyUrl)
                    handler.Proxy = new WebProxy(proxyUrl);

                _client = new HttpClient(handler, disposeHandler: true)
                {
                    Timeout = config.Timeout
                };
                _cookies = handler.CookieContainer;
                _config = handlerConfig;
            }
            catch
            {
                handler.Dispose();
                throw;
            }
        }

        // NOTE! While the following await is not strictly required (the task object could have been
        // returned to the caller directly), it ensures that the client and handler states (like
        // cookie container population) are up to date before this method ends (also avoids
        // surprises during debugging).

        return await _client.SendAsync(request, completionOption, cancellationToken).ConfigureAwait(false);
    }

    public void Dispose() => _client?.Dispose();
}
