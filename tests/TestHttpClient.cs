namespace WebLinq.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;

    sealed class TestHttpClient : IHttpClient
    {
        readonly Queue<HttpResponseMessage> _responses;
        readonly Queue<HttpRequestMessage> _requests;
        readonly Queue<HttpConfig> _requestConfigs;

        public TestHttpClient(params HttpResponseMessage[] responses) :
            this(HttpConfig.Default, responses) {}

        public TestHttpClient(HttpConfig config, params HttpResponseMessage[] responses) :
            this(config, new Queue<HttpResponseMessage>(responses),
                new Queue<HttpRequestMessage>(),
                new Queue<HttpConfig>()) {}

        TestHttpClient(HttpConfig config,
            Queue<HttpResponseMessage> responses,
            Queue<HttpRequestMessage> requests,
            Queue<HttpConfig> requestConfigs)
        {
            Config = config;
            _responses = responses;
            _requests = requests;
            _requestConfigs = requestConfigs;
        }

        public HttpRequestMessage DequeueRequestMessage() =>
            DequeueRequest((rm, _) => rm);

        public T DequeueRequest<T>(Func<HttpRequestMessage, HttpConfig, T> selector)
        {
            var config = _requestConfigs.Dequeue();
            var request = _requests.Dequeue();
            return selector(request, config);
        }

        public HttpConfig Config { get; }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpConfig config)
        {
            _requestConfigs.Enqueue(config);
            _requests.Enqueue(request);
            var response = _responses.Dequeue();
            response.RequestMessage = request;
            return Task.FromResult(response);
        }

        public IHttpClient WithConfig(HttpConfig config) =>
            Config == config
                ? this
                : new TestHttpClient(config, _responses, _requests, _requestConfigs);
    }
}