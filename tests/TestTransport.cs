#pragma warning disable CA2000 // Dispose objects before losing scope (FIXME)

namespace WebLinq.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    sealed class TestTransport : IHttpClient
    {
        readonly Queue<HttpResponseMessage> _responses;
        readonly Queue<HttpRequestMessage> _requests;
        readonly Queue<HttpConfig> _requestConfigs;

        public TestTransport(params HttpResponseMessage[] responses)
        {
            _responses      = new Queue<HttpResponseMessage>(responses);
            _requests       = new Queue<HttpRequestMessage>();
            _requestConfigs = new Queue<HttpConfig>();
        }

        public TestTransport Enqueue(HttpResponseMessage response)
        {
            _responses.Enqueue(response);
            return this;
        }

        public TestTransport EnqueueHtml(string html, HttpStatusCode statusCode = HttpStatusCode.OK) =>
            Enqueue(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(html, Encoding.UTF8, "text/html")
            });

        public TestTransport EnqueueText(string text, HttpStatusCode statusCode = HttpStatusCode.OK) =>
            Enqueue(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(text, Encoding.UTF8, "text/plain")
            });

        public TestTransport EnqueueJson(string json, HttpStatusCode statusCode = HttpStatusCode.OK) =>
            Enqueue(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        public TestTransport Enqueue(byte[] bytes, HttpStatusCode statusCode = HttpStatusCode.OK) =>
            Enqueue(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new ByteArrayContent(bytes)
            });

        public HttpRequestMessage DequeueRequestMessage() =>
            DequeueRequest((rm, _) => rm);

        public T DequeueRequest<T>(Func<HttpRequestMessage, HttpConfig, T> selector)
        {
            var config = _requestConfigs.Dequeue();
            var request = _requests.Dequeue();
            return selector(request, config);
        }

        async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpConfig config)
        {
            _requestConfigs.Enqueue(config);
            _requests.Enqueue(await request.CloneAsync());
            var response = _responses.Dequeue();
            response.RequestMessage = request;
            return response;
        }

        public CookieContainer GetCookieContainer() => throw new NotImplementedException();

        public Task<HttpResponseMessage> SendAsync(HttpConfig config, HttpRequestMessage request,
                                                   HttpCompletionOption completionOption,
                                                   CancellationToken cancellationToken) =>
            SendAsync(request, config);

        public void Dispose() { }
    }

    file static class Extensions
    {
        public static async Task<HttpRequestMessage> CloneAsync(this HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri);

            foreach (var (key, value) in request.Headers)
                clone.Headers.TryAddWithoutValidation(key, value);

            if (request.Content is { } content)
            {
                clone.Content = new ReadOnlyMemoryContent(await content.ReadAsByteArrayAsync().ConfigureAwait(false));

                foreach (var (key, value) in content.Headers)
                    clone.Content.Headers.TryAddWithoutValidation(key, value);
            }

            return clone;
        }
    }
}
