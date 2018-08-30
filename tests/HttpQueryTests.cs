﻿using NUnit.Framework;
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
using static WebLinq.Modules.HttpModule;

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
        public async Task WithHeaderGetRequestSetHeaderTest()
        {
            var http = new TestHttpClient(HttpConfig.Default.WithHeader("name1","value1"),
                new HttpResponseMessage()
                {
                    Content = new ByteArrayContent(new byte[0]),
                });

            var result = await http.SetHeader("name2", "value2").Get(new Uri("https://www.example.com"));
            var request = ((TestHttpClient)result.Client).DequeueRequest((m, c) => new { Message = m, c.Headers });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));

            Assert.That(http.Config.Headers.Single().Key, Is.EqualTo("name1"));
            Assert.That(http.Config.Headers.Single().Value.Single(), Is.EqualTo("value1"));
            Assert.That(request.Headers.Keys, Is.EquivalentTo(new[] { "name1", "name2" }));
            Assert.That(request.Headers.Values.Select(v => v.Single()), Is.EquivalentTo(new[] { "value1","value2"}));
        }

        [Test]
        public async Task GetRequestsSetSameHeaderTest()
        {
            var http = new TestHttpClient(HttpConfig.Default.WithHeader("foo", "bar"),
                new HttpResponseMessage()
                {
                    Content = new ByteArrayContent(new byte[0]),
                });

            var result = await http.SetHeader("foo", "bar").Get(new Uri("https://www.example.com"));
            var request = ((TestHttpClient)result.Client).DequeueRequest((m, c) => new { Message = m, c.Headers });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));

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

        [Test]
        public async Task SeparateRequestsWithDifferentHeaders()
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

            var result1 = await http.SetHeader("h1", "v1").Get(new Uri("https://www.example.com"));
            var result2 = await http.SetHeader("h2", "v2").Get(new Uri("https://www.example.com/page"));

            var request1 = ((TestHttpClient)result1.Client).DequeueRequest((m, c) => new { Message = m, c.Headers });
            var request2 = ((TestHttpClient)result2.Client).DequeueRequest((m, c) => new { Message = m, c.Headers });

            Assert.That(request1.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request1.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/page")));

            Assert.That(request1.Headers.Single().Key, Is.EqualTo("h1"));
            Assert.That(request2.Headers.Single().Key, Is.EqualTo("h2"));
            Assert.That(request1.Headers.Single().Value.Single(), Is.EqualTo("v1"));
            Assert.That(request2.Headers.Single().Value.Single(), Is.EqualTo("v2"));
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

        [Test]
        public async Task ChainedGetRequestsWithCookie()
        {
            var http = new TestHttpClient(HttpConfig.Default.WithCookies(new[] { new Cookie("name", "value") }),
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
            Assert.That(request1.Config.Cookies.Single(), Is.EqualTo(new Cookie("name", "value")));
            Assert.That(request2.Config.Cookies.Single(), Is.EqualTo(new Cookie("name", "value")));
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
            Assert.That(await request.Message.Content.ReadAsStringAsync(), Is.EqualTo("name=value"));
        }

        [Test]
        public async Task PostRequestWithCookie()
        {
            var http = new TestHttpClient(HttpConfig.Default.WithCookies(new[] { new Cookie("name", "value") }),
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

        [Test]
        public async Task SubmitDefaultMethodIsGetTest()
        {
            var action = "/action_page.php";
            var http = new TestHttpClient(
                new HttpResponseMessage()
                {
                    Content = new StringContent("<!DOCTYPE html>" +
                                                 "<html>" +
                                                 "<body>" +
                                                 "<h2> HTML Forms </h2>" +
                                                 "<form action=\"" + action + "\">" +
                                                 "  First name:<br>" +
                                                 "  <input type=\"text\" name=\"firstname\" value=\"Mickey\" >" +
                                                 "  <br >" +
                                                 "  Last name:<br>" +
                                                 "  <input type=\"text\" name=\"lastname\" value=\"Mouse\" >" +
                                                 "  <br><br>" +
                                                 "  <input type=\"submit\" value=\"Submit\" >" +
                                                 "</form>" +
                                                 "</body>" +
                                                 "</html>", Encoding.UTF8, "text/html"),
                },
                new HttpResponseMessage()
                {
                    Content = new ByteArrayContent(new byte[0]),
                });
            var data = new NameValueCollection()
            {
                ["firstname"] = "Mickey",
                ["lastname"] = "Mouse"
            };

            var result = await http.Get(new Uri("https://www.example.com"))
                                   .Submit(0, data);

            var request1 = ((TestHttpClient)result.Client).DequeueRequest((m, c) => new { Message = m, Config = c });
            var request2 = ((TestHttpClient)result.Client).DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request1.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request1.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com" + action + "?firstname=Mickey&lastname=Mouse")));
        }


        [Test]
        public async Task SubmitPostTest()
        {
            var action = "/action_page.php";
            var http = new TestHttpClient(
                new HttpResponseMessage()
                {
                    Content = new StringContent("<!DOCTYPE html>" +
                                                 "<html>" +
                                                 "<body>" +
                                                 "<h2> HTML Forms </h2>" +
                                                 "<form method='post' action=\"" + action + "\">" +
                                                 "  First name:<br>" +
                                                 "  <input type=\"text\" name=\"firstname\" value=\"Mickey\" >" +
                                                 "  <br >" +
                                                 "  Last name:<br>" +
                                                 "  <input type=\"text\" name=\"lastname\" value=\"Mouse\" >" +
                                                 "  <br><br>" +
                                                 "  <input type=\"submit\" value=\"Submit\" >" +
                                                 "</form>" +
                                                 "</body>" +
                                                 "</html>", Encoding.UTF8, "text/html"),
                },
                new HttpResponseMessage()
                {
                    Content = new ByteArrayContent(new byte[0]),
                });
            var data = new NameValueCollection()
            {
                ["firstname"] = "Mickey",
                ["lastname"] = "Mouse"
            };

            var result = await http.Get(new Uri("https://www.example.com"))
                                   .Submit(0, data);

            var request1 = ((TestHttpClient)result.Client).DequeueRequest((m, c) => new { Message = m, Config = c });
            var request2 = ((TestHttpClient)result.Client).DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request1.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request1.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com" + action)));
            Assert.That(await request2.Message.Content.ReadAsStringAsync(), Is.EqualTo("firstname=Mickey&lastname=Mouse"));
        }

        [Test]
        public async Task SubmitToTest()
        {
            var action = "/action_page.php";
            var html = "<!DOCTYPE html>" +
                                                 "<html>" +
                                                 "<body>" +
                                                 "<h2> HTML Forms </h2>" +
                                                 "<form action=\"" + action + "\">" +
                                                 "  First name:<br>" +
                                                 "  <input type=\"text\" name=\"firstname\" value=\"Mickey\" >" +
                                                 "  <br >" +
                                                 "  Last name:<br>" +
                                                 "  <input type=\"text\" name=\"lastname\" value=\"Mouse\" >" +
                                                 "  <br><br>" +
                                                 "  <input type=\"submit\" value=\"Submit\" >" +
                                                 "</form>" +
                                                 "</body>" +
                                                 "</html>";
            var http = new TestHttpClient(
                new HttpResponseMessage()
                {
                    Content = new ByteArrayContent(new byte[0]),
                });
            var data = new NameValueCollection()
            {
                ["firstname"] = "Mickey",
                ["lastname"] = "Mouse"
            };
            var result = await http.SubmitTo(new Uri("https://www.example.org"), 
                Html.HtmlParser.Default.Parse(html, new Uri("https://www.example.com")), 
                0,
                data);
            var request = ((TestHttpClient)result.Client).DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.org" + "?firstname=Mickey&lastname=Mouse")));
            Assert.That(request.Message.Headers, Is.Empty);
        }

        [Test]
        public async Task SubmitToPostRequestTest()
        {
            var action = "/action_page.php";
            var html = "<!DOCTYPE html>" +
                       "<html>" +
                       "<body>" +
                       "<h2> HTML Forms </h2>" +
                       "<form method='post' action=\"" + action + "\">" +
                       "  First name:<br>" +
                       "  <input type=\"text\" name=\"firstname\" value=\"Mickey\" >" +
                       "  <br >" +
                       "  Last name:<br>" +
                       "  <input type=\"text\" name=\"lastname\" value=\"Mouse\" >" +
                       "  <br><br>" +
                       "  <input type=\"submit\" value=\"Submit\" >" +
                       "</form>" +
                       "</body>" +
                       "</html>";

            var http = new TestHttpClient(
                new HttpResponseMessage()
                {
                    Content = new ByteArrayContent(new byte[0]),
                });
            var data = new NameValueCollection()
            {
                ["firstname"] = "Mickey",
                ["lastname"] = "Mouse"
            };
            var result = await http.SubmitTo(new Uri("https://www.example.org"),
                Html.HtmlParser.Default.Parse(html, new Uri("https://www.example.com")), 
                0,
                data);
            var request = ((TestHttpClient)result.Client).DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.org")));
            Assert.That(request.Message.Content.Headers.GetValues("Content-Type").Single(), Is.EqualTo("application/x-www-form-urlencoded"));
            Assert.That(await request.Message.Content.ReadAsStringAsync(), Is.EqualTo("firstname=Mickey&lastname=Mouse"));
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
