namespace WebLinq.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Modules;

    sealed class TestTransport
    {
        readonly Queue<HttpResponseMessage> _responses;
        readonly Queue<HttpRequestMessage> _requests;
        readonly Queue<HttpConfig> _requestConfigs;

        public TestTransport(params HttpResponseMessage[] responses) :
            this(HttpConfig.Default, responses) {}

        public TestTransport(HttpConfig config, params HttpResponseMessage[] responses)
        {
            _responses      = new Queue<HttpResponseMessage>(responses);
            _requests       = new Queue<HttpRequestMessage>();
            _requestConfigs = new Queue<HttpConfig>();

            Http = HttpModule.Http.WithConfig(config)
                                  .Wrap((_, req, cfg) => SendAsync(req, cfg));
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

        public IHttpClient Http { get; }

        Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpConfig config)
        {
            _requestConfigs.Enqueue(config);
            _requests.Enqueue(request);
            var response = _responses.Dequeue();
            response.RequestMessage = request;
            return Task.FromResult(response);
        }
    }
}