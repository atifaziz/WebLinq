using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.PlatformServices;
using System.Reactive.Linq;
using System.Reactive.Threading;
using System.Collections.Specialized;
using Mannex.Collections.Generic;
using System.Net;

namespace WebLinq.Tests
{
    public class HttpQueryTests
    {

        [Test]
        public async Task GetRequestTest()
        {
            var http = new TestHttpClient(
                new HttpResponseMessage
                {
                    Content = new ByteArrayContent(new byte[0]),
                });

            var result = await http.Get(new Uri("https://www.example.com"));
            var request = ((TestHttpClient)result.Client).DequeueRequestMessage();

            Console.WriteLine(request.Content);

            Assert.That(request.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));
            Assert.That(request.Headers, Is.Empty);
            Assert.That(request.Properties.Count, Is.Zero);
        }

        [Test]
        public async Task GetRequestSetHeaderTest()
        {
            var http = new TestHttpClient(HttpConfig.Default.WithHeaders(HttpHeaderCollection.Empty),
                new HttpResponseMessage()
                {
                    Content = new ByteArrayContent(new byte[0]),
                });

            var result = await http.SetHeader("foo", "bar").Get(new Uri("https://www.example.com"));
            var request = ((TestHttpClient)result.Client).DequeueRequest((m, c) => new { Message = m, c.Headers });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));

            Assert.That(http.Config.Headers, Is.Empty);
            Assert.That(request.Headers.Single().Key, Is.EqualTo("foo"));
            Assert.That(request.Headers.Single().Value.Single(), Is.EqualTo("bar"));
        }

        [Test]
        public async Task ChainedGetRequests()
        {
            var http = new TestHttpClient(HttpConfig.Default,
                new HttpResponseMessage()
                {
                    Content = new ByteArrayContent(new byte[0]),
                },
                new HttpResponseMessage()
                {
                    Content = new ByteArrayContent(new byte[0]),
                });

            var result = await http.Get(new Uri("https://www.example.com"))
                                   .Get(new Uri("https://www.example.com/page"));
            var request1 = ((TestHttpClient)result.Client).DequeueRequest((m, c) => new { Message = m, Config = c });
            var request2 = ((TestHttpClient)result.Client).DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request1.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request1.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/page")));
            Assert.That(request1.Config, Is.SameAs(HttpConfig.Default));
            Assert.That(request2.Config, Is.SameAs(HttpConfig.Default));
        }

        [Test]
        public async Task ChainedRequestsWithDifferentHeaders()
        {
            var http = new TestHttpClient(
                new HttpResponseMessage()
                {
                    Content = new ByteArrayContent(new byte[0]),
                },
                new HttpResponseMessage()
                {
                    Content = new ByteArrayContent(new byte[0]),
                });

            var result = await http.SetHeader("h1", "v1")
                                   .Get(new Uri("https://www.example.com"))
                                   .SetHeader("h2", "v2")
                                   .Get(new Uri("https://www.example.com/page"));

            var request1 = ((TestHttpClient)result.Client).DequeueRequest((m, c) => new { Message = m, c.Headers });
            var request2 = ((TestHttpClient)result.Client).DequeueRequest((m, c) => new { Message = m, c.Headers });

            Assert.That(request1.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request1.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/page")));

            Assert.That(request1.Headers.Single().Key, Is.EqualTo("h1"));
            Assert.That(request2.Headers.Keys, Is.EquivalentTo(new[] { "h1", "h2" }));
            Assert.That(request1.Headers.Single().Value.Single(), Is.EqualTo("v1"));
            Assert.That(request2.Headers.Values.Select(v => v.Single()), Is.EquivalentTo(new[] { "v1", "v2" }));
        }

        }

        [Test]
        public async Task GetRequestWithHeaderTest()
        {
            var http = new TestHttpClient(HttpConfig.Default.WithHeader("foo", "bar"),
                new HttpResponseMessage()
                {
                    Content = new ByteArrayContent(new byte[0]),
                });

            var result = await http.Get(new Uri("https://www.example.com"));
            var request = ((TestHttpClient)result.Client).DequeueRequest((m, c) => new { Message = m, Config = c});

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));
            Assert.That(request.Config.Headers.Single().Key, Is.EqualTo("foo"));
            Assert.That(request.Config.Headers.Single().Value.Single(), Is.EqualTo("bar"));
        }

        [Test]
        public async Task GetRequestWithUserAgentTest()
        {
            var http = new TestHttpClient(HttpConfig.Default.WithUserAgent("Spider/1.0"),
                new HttpResponseMessage()
                {
                    Content = new ByteArrayContent(new byte[0]),
                });

            var result = await http.Get(new Uri("https://www.example.com"));
            var request = ((TestHttpClient)result.Client).DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));
            Assert.That(request.Config.UserAgent, Is.EqualTo("Spider/1.0"));
        }

        [Test]
        public async Task GetRequestWithCredentialsTest()
        {
            var credentials = new NetworkCredential("admin", "admin");
            var http = new TestHttpClient(HttpConfig.Default.WithCredentials(credentials),
                new HttpResponseMessage()
                {
                    Content = new ByteArrayContent(new byte[0]),
                });

            var result = await http.Get(new Uri("https://www.example.com"));
            var request = ((TestHttpClient)result.Client).DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));
            Assert.That(request.Config.Credentials, Is.SameAs(credentials));
        }

        [Test]
        public async Task GetRequestWithTimeout()
        {
            var http = new TestHttpClient(HttpConfig.Default.WithTimeout(new TimeSpan(0,1,0)),
                new HttpResponseMessage()
                {
                    Content = new ByteArrayContent(new byte[0]),
                });

            var result = await http.Get(new Uri("https://www.example.com"));
            var request = ((TestHttpClient)result.Client).DequeueRequest((m, c) => new { Message = m, Config = c });
        
            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));
            Assert.That(request.Config.Timeout, Is.EqualTo(new TimeSpan(0,1,0)));
        }

        [Test]
        public async Task GetRequestWithCookies()
        {
            var http = new TestHttpClient(HttpConfig.Default.WithCookies(new[] { new Cookie("name", "value") }),
                new HttpResponseMessage()
                {
                    Content = new ByteArrayContent(new byte[0]),
                });

            var result = await http.Get(new Uri("https://www.example.com"));
            var request = ((TestHttpClient)result.Client).DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));
            Assert.That(request.Config.Cookies.Single(), Is.EqualTo(new Cookie("name", "value")));
        }

        }

        [Test]
        public async Task PostRequestTest()
        {
            var http = new TestHttpClient(
                new HttpResponseMessage()
                {
                    Content = new ByteArrayContent(new byte[0]),
                });

            var data = new NameValueCollection() { ["name"] = "value" };
            
            var result = await http.Post(new Uri("https://www.example.com"), data);
            var request = ((TestHttpClient)result.Client).DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));

            Assert.That(request.Config.Headers, Is.Empty);
            Assert.That(request.Message.Properties.Count, Is.Zero);
            Assert.That(await request.Message.Content.ReadAsStringAsync(), Is.EqualTo("name=value"));
        }

        [Test]
        public async Task PostRequestWith2NameValuePairs()
        {
            var http = new TestHttpClient(
                new HttpResponseMessage()
                {
                    Content = new ByteArrayContent(new byte[0]),
                });

            var data = new NameValueCollection()
            {
                ["name1"] = "value1",
                ["name2"] = "value2",
            };

            var result = await http.Post(new Uri("https://www.example.com"), data);
            var request = ((TestHttpClient)result.Client).DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));

            Assert.That(request.Config.Headers, Is.Empty);
            Assert.That(request.Message.Properties.Count, Is.Zero);
            Assert.That(await request.Message.Content.ReadAsStringAsync(), Is.EqualTo("name1=value1&name2=value2"));
        }

        [Test]
        public async Task PostRequestWithAmpersandInNameValuePair()
        {
            var http = new TestHttpClient(
                new HttpResponseMessage()
                {
                    Content = new ByteArrayContent(new byte[0]),
                });

            var data = new NameValueCollection() { ["foo&bar"] = "baz" };

            var result = await http.Post(new Uri("https://www.example.com"), data);
            var request = ((TestHttpClient)result.Client).DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));

            Assert.That(request.Config.Headers, Is.Empty);
            Assert.That(request.Message.Properties.Count, Is.Zero);
            Assert.That(await request.Message.Content.ReadAsStringAsync(), Is.EqualTo("foo%26bar=baz"));
        }

    }

    public class TestHttpClient : IHttpClient
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
