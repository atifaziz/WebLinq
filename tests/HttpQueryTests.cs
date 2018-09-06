namespace WebLinq.Tests
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Reactive.Linq;
    using System.Collections.Specialized;
    using System.Net;
    using System.Reactive.Threading.Tasks;
    using NUnit.Framework;
    using static Modules.HttpModule;

    public class HttpQueryTests
    {

        [Test]
        public async Task AcceptSingleMediaType()
        {
            var tt = new TestTransport().EnqueueText(string.Empty);

            await tt.Http.Get(new Uri("https://www.example.com/"))
                         .Accept("text/plain");

            // Succeeds if it doesn't throw.
        }

        [Test]
        public async Task AcceptMultipleMediaTypes()
        {
            var tt = new TestTransport().EnqueueJson("{}");

            await tt.Http.Get(new Uri("https://www.example.com/"))
                         .Accept("text/json", "application/json");

            // Succeeds if it doesn't throw.
        }

        [Test]
        public void AcceptThrowsExceptionOnMediaTypeMismatch()
        {
            var tt = new TestTransport().EnqueueText("foo");

            Assert.ThrowsAsync<Exception>(() =>
                tt.Http.Get(new Uri("https://www.example.com/"))
                    .Accept("text/html")
                    .ToTask());
        }

        [Test]
        public void AcceptThrowsExceptionOnEmptyString()
        {
            var tt = new TestTransport().Enqueue(new byte[0]);

            Assert.ThrowsAsync<Exception>(() =>
                tt.Http.Get(new Uri("https://www.example.com/"))
                    .Accept("")
                    .ToTask());
        }

        [Test]
        public async Task AcceptNoParameterTest()
        {
            var tt = new TestTransport().Enqueue(new byte[0]);

            await tt.Http.Get(new Uri("https://www.example.com/"))
                         .Accept();

            // Succeeds if it doesn't throw.
        }

        [Test]
        public async Task GetRequestTest()
        {
            var tt = new TestTransport().Enqueue(new byte[0]);

            await tt.Http.Get(new Uri("https://www.example.com/"));
            var request = tt.DequeueRequestMessage();

            Assert.That(request.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request.Headers, Is.Empty);
        }

        [Test]
        public async Task GetRequestSetHeaderTest()
        {
            var tt = new TestTransport(HttpConfig.Default.WithHeaders(HttpHeaderCollection.Empty));
            tt.Enqueue(new byte[0]);

            await tt.Http.SetHeader("foo", "bar").Get(new Uri("https://www.example.com/"));
            var request = tt.DequeueRequest((m, c) => new { Message = m, c.Headers });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(tt.Http.Config.Headers, Is.Empty);
            Assert.That(request.Headers.Single().Key, Is.EqualTo("foo"));
            Assert.That(request.Headers.Single().Value.Single(), Is.EqualTo("bar"));
        }

        [TestCase(HttpStatusCode.NotFound)]
        [TestCase(HttpStatusCode.NotImplemented)]
        [TestCase(HttpStatusCode.BadGateway)]
        [TestCase(HttpStatusCode.GatewayTimeout)]
        [TestCase(HttpStatusCode.Forbidden)]
        public void NotFoundFetchThrowsException(HttpStatusCode statusCode)
        {
            var tt = new TestTransport().Enqueue(new byte[0], statusCode);

            Assert.ThrowsAsync<HttpRequestException>(() =>
                tt.Http.Get(new Uri("https://www.example.com/")).ToTask());
        }

        [TestCase(HttpStatusCode.NotFound)]
        [TestCase(HttpStatusCode.NotImplemented)]
        [TestCase(HttpStatusCode.BadGateway)]
        [TestCase(HttpStatusCode.GatewayTimeout)]
        [TestCase(HttpStatusCode.Forbidden)]
        public async Task NotFoundFetchReturnErroneousFetchTest(HttpStatusCode statusCode)
        {
            var tt = new TestTransport().Enqueue(new byte[0], statusCode);

            var result = await tt.Http.Get(new Uri("https://www.example.com/"))
                                      .ReturnErroneousFetch();

            Assert.That(result.RequestUrl, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(result.StatusCode, Is.EqualTo(statusCode));
        }

        [Test]
        public async Task SetCookieHeaderInResponseTest()
        {
            var tt = new TestTransport()
                .Enqueue(new byte[0])
                .Enqueue(new HttpResponseMessage
                {
                    Headers =
                    {
                        { "Set-Cookie", "foo=bar" }
                    },
                    Content = new ByteArrayContent(new byte[0]),
                });

            var result = await tt.Http.Get(new Uri("https://www.example.com/"))
                                   .Get(new Uri("https://www.example.com/page"));

            var request1 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request1.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request1.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/page")));
            Assert.That(result.Client.Config.Cookies.Single().Name, Is.EqualTo("foo"));
            Assert.That(result.Client.Config.Cookies.Single().Value, Is.EqualTo("bar"));
        }


        [Test, Ignore("Currently not handled by WebLinq")]
        public async Task SameCookiesDifferentDomainsKeptInConfiguration()
        {
            var tt = new TestTransport(
                new HttpResponseMessage
                {
                    Headers =
                    {
                        { "Set-Cookie", "foo=bar" }
                    },
                    Content = new ByteArrayContent(new byte[0]),
                },
                new HttpResponseMessage
                {
                    Headers =
                    {
                        { "Set-Cookie", "foo=bar" }
                    },
                    Content = new ByteArrayContent(new byte[0]),
                });

            var result = await tt.Http.Get(new Uri("https://www.example.com/"))
                                   .Get(new Uri("https://www.google.com/"));
            var request1 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request1.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request1.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.google.com/")));
            Assert.That(result.Client.Config.Cookies.Single(c => c.Domain.Equals("www.example.com")).Name, Is.EqualTo("foo"));
            Assert.That(result.Client.Config.Cookies.Single(c => c.Domain.Equals("www.example.com")).Value, Is.EqualTo("bar"));
            Assert.That(result.Client.Config.Cookies.Single(c => c.Domain.Equals("www.google.com")).Name, Is.EqualTo("foo"));
            Assert.That(result.Client.Config.Cookies.Single(c => c.Domain.Equals("www.google.com")).Value, Is.EqualTo("bar"));
        }

        [Test]
        public async Task CookiesKeptInSubdomainWhenSpecified()
        {
            var tt = new TestTransport()
                .Enqueue(new byte[0])
                .Enqueue(new HttpResponseMessage
                {
                    Headers =
                    {
                        { "Set-Cookie", "foo=bar;Domain=example.com" }
                    },
                    Content = new ByteArrayContent(new byte[0]),
                });

            var result = await
                tt.Http.Get(new Uri("https://www.example.com/"))
                               .Get(new Uri("https://mail.example.com/"));

            var request1 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request1.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request1.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://mail.example.com/")));
            Assert.That(result.Client.Config.Cookies.Single().Name, Is.EqualTo("foo"));
            Assert.That(result.Client.Config.Cookies.Single().Value, Is.EqualTo("bar"));
        }

        [Test]
        public async Task WithHeaderGetRequestSetHeaderTest()
        {
            var tt = new TestTransport(HttpConfig.Default.WithHeader("name1", "value1"))
                .Enqueue(new byte[0]);

            await tt.Http.SetHeader("name2", "value2").Get(new Uri("https://www.example.com/"));
            var request = tt.DequeueRequest((m, c) => new { Message = m, c.Headers });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(tt.Http.Config.Headers.Single().Key, Is.EqualTo("name1"));
            Assert.That(tt.Http.Config.Headers.Single().Value.Single(), Is.EqualTo("value1"));
            Assert.That(request.Headers.Keys, Is.EquivalentTo(new[] { "name1", "name2" }));
            Assert.That(request.Headers.Values.Select(v => v.Single()), Is.EquivalentTo(new[] { "value1","value2"}));
        }

        [Test]
        public async Task GetRequestSetSameHeaderAsConfiguration()
        {
            var tt = new TestTransport(HttpConfig.Default.WithHeader("foo", "bar"))
                .Enqueue(new byte[0]);

            await tt.Http.SetHeader("foo", "bar").Get(new Uri("https://www.example.com/"));
            var request = tt.DequeueRequest((m, c) => new { Message = m, c.Headers });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request.Headers.Single().Key, Is.EqualTo("foo"));
            Assert.That(request.Headers.Single().Value.Single(), Is.EqualTo("bar"));
        }

        [Test]
        public async Task ChainedGetRequests()
        {
            var tt = new TestTransport()
                .Enqueue(new byte[0])
                .Enqueue(new byte[0]);

            await tt.Http.Get(new Uri("https://www.example.com/"))
                         .Get(new Uri("https://www.example.com/page"));
            var request1 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request1.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request1.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/page")));
            Assert.That(request1.Config, Is.SameAs(HttpConfig.Default));
            Assert.That(request2.Config, Is.SameAs(HttpConfig.Default));
        }

        [Test, Ignore("https://github.com/weblinq/WebLinq/issues/18")]
        public async Task ChainedGetRequestsWithDifferentHeaders()
        {
            var tt = new TestTransport()
                .Enqueue(new byte[0])
                .Enqueue(new byte[0]);

            await tt.Http.SetHeader("h1", "v1")
                         .Get(new Uri("https://www.example.com/"))
                         .SetHeader("h2", "v2")
                         .Get(new Uri("https://www.example.com/page"));

            var request1 = tt.DequeueRequest((m, c) => new { Message = m, c.Headers });
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, c.Headers });

            Assert.That(request1.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request1.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/page")));
            Assert.That(request1.Headers.Single().Key, Is.EqualTo("h1"));
            Assert.That(request2.Headers.Keys, Is.EquivalentTo(new[] { "h1", "h2" }));
            Assert.That(request1.Headers.Single().Value.Single(), Is.EqualTo("v1"));
            Assert.That(request2.Headers.Values.Select(v => v.Single()), Is.EquivalentTo(new[] { "v1", "v2" }));
        }

        [Test]
        public async Task SeparateRequestsWithDifferentHeaders()
        {
            var tt = new TestTransport()
                .Enqueue(new byte[0])
                .Enqueue(new byte[0]);

            await tt.Http.SetHeader("h1", "v1").Get(new Uri("https://www.example.com/"));
            await tt.Http.SetHeader("h2", "v2").Get(new Uri("https://www.example.com/page"));

            var request1 = tt.DequeueRequest((m, c) => new { Message = m, c.Headers });
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, c.Headers });

            Assert.That(request1.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request1.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/page")));
            Assert.That(request1.Headers.Single().Key, Is.EqualTo("h1"));
            Assert.That(request2.Headers.Single().Key, Is.EqualTo("h2"));
            Assert.That(request1.Headers.Single().Value.Single(), Is.EqualTo("v1"));
            Assert.That(request2.Headers.Single().Value.Single(), Is.EqualTo("v2"));
        }

        [Test, Ignore("https://github.com/weblinq/WebLinq/issues/18")]
        public async Task ChainedGetRequestsWithDifferentHeaders2()
        {
            var tt = new TestTransport(HttpConfig.Default.WithHeader("h1","v1"))
                .Enqueue(new byte[0])
                .Enqueue(new byte[0]);

            await tt.Http.Get(new Uri("https://www.example.com/"))
                         .SetHeader("h2", "v2")
                         .Get(new Uri("https://www.example.com/page"));

            var request1 = tt.DequeueRequest((m, c) => new { Message = m, c.Headers });
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, c.Headers });

            Assert.That(request1.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request1.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/page")));
            Assert.That(request1.Headers.Single().Key, Is.EqualTo("h1"));
            Assert.That(request1.Headers.Single().Value.Single(), Is.EqualTo("v1"));
            Assert.That(request2.Headers.Keys, Is.EquivalentTo(new[] { "h1", "h2" }));
            Assert.That(request2.Headers.Values, Is.EquivalentTo(new[] { "v1", "v2" }));
        }

        [Test]
        public async Task GetRequestWithHeader()
        {
            var tt = new TestTransport(HttpConfig.Default.WithHeader("foo", "bar"))
                .Enqueue(new byte[0]);

            await tt.Http.Get(new Uri("https://www.example.com/"));
            var request = tt.DequeueRequest((m, c) => new { Message = m, Config = c});

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request.Config.Headers.Single().Key, Is.EqualTo("foo"));
            Assert.That(request.Config.Headers.Single().Value.Single(), Is.EqualTo("bar"));
        }

        [Test]
        public async Task GetRequestWithUserAgent()
        {
            var tt = new TestTransport(HttpConfig.Default.WithUserAgent("Spider/1.0"))
                .Enqueue(new byte[0]);

            await tt.Http.Get(new Uri("https://www.example.com/"));
            var request = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request.Config.UserAgent, Is.EqualTo("Spider/1.0"));
        }

        [Test]
        public async Task GetRequestWithCredentials()
        {
            var credentials = new NetworkCredential("admin", "admin");
            var tt = new TestTransport(HttpConfig.Default.WithCredentials(credentials))
                .Enqueue(new byte[0]);

            await tt.Http.Get(new Uri("https://www.example.com/"));
            var request = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request.Config.Credentials, Is.SameAs(credentials));
        }

        [Test]
        public async Task GetRequestWithTimeout()
        {
            var tt = new TestTransport(HttpConfig.Default.WithTimeout(new TimeSpan(0, 1, 0)))
                .Enqueue(new byte[0]);

            await tt.Http.Get(new Uri("https://www.example.com/"));
            var request = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request.Config.Timeout, Is.EqualTo(new TimeSpan(0, 1, 0)));
        }

        [Test]
        public async Task GetRequestWithCookies()
        {
            var cookie = new Cookie("name", "value");
            var tt = new TestTransport(HttpConfig.Default.WithCookies(new[] { cookie }))
                .Enqueue(new byte[0]);

            await tt.Http.Get(new Uri("https://www.example.com/"));
            var request = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request.Config.Cookies.Single(), Is.SameAs(cookie));
        }

        [Test]
        public async Task ChainedGetRequestsWithCookie()
        {
            var cookie = new Cookie("name", "value");
            var tt = new TestTransport(HttpConfig.Default.WithCookies(new[] { cookie }))
                .Enqueue(new byte[0])
                .Enqueue(new byte[0]);

            await tt.Http.Get(new Uri("https://www.example.com/"))
                         .Get(new Uri("https://www.example.com/page"));

            var request1 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request1.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request1.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/page")));
            Assert.That(request1.Config.Cookies.Single(), Is.SameAs(cookie));
            Assert.That(request2.Config.Cookies.Single(), Is.SameAs(cookie));
        }

        [Test]
        public async Task ChainedGetRequestsWithCredentials()
        {
            var credentials = new NetworkCredential("admin", "admin");
            var tt = new TestTransport(HttpConfig.Default.WithCredentials(credentials))
                .Enqueue(new byte[0])
                .Enqueue(new byte[0]);

            await tt.Http.Get(new Uri("https://www.example.com/"))
                         .Get(new Uri("https://www.example.com/page"));

            var request1 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request1.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request1.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/page")));
            Assert.That(request1.Config.Credentials, Is.SameAs(credentials));
            Assert.That(request2.Config.Credentials, Is.SameAs(credentials));
        }

        [Test]
        public async Task ChainedGetRequestsWithHeader()
        {
            var tt = new TestTransport(HttpConfig.Default.WithHeader("name","value"))
                .Enqueue(new byte[0])
                .Enqueue(new byte[0]);

            await tt.Http.Get(new Uri("https://www.example.com/"))
                         .Get(new Uri("https://www.example.com/page"));

            var request1 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request1.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request1.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/page")));
            Assert.That(request1.Config.Headers.Single().Key, Is.EqualTo("name"));
            Assert.That(request2.Config.Headers.Single().Key, Is.EqualTo("name"));
            Assert.That(request1.Config.Headers.Single().Value.Single(), Is.EqualTo("value"));
            Assert.That(request2.Config.Headers.Single().Value.Single(), Is.EqualTo("value"));
        }

        [Test]
        public async Task ChainedGetRequestsWithTimeout()
        {
            var tt = new TestTransport(HttpConfig.Default.WithTimeout(new TimeSpan(0,1,0)))
                .Enqueue(new byte[0])
                .Enqueue(new byte[0]);

            await tt.Http.Get(new Uri("https://www.example.com/"))
                         .Get(new Uri("https://www.example.com/page"));

            var request1 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request1.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request1.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/page")));
            Assert.That(request1.Config.Timeout, Is.EqualTo(new TimeSpan(0, 1, 0)));
            Assert.That(request2.Config.Timeout, Is.EqualTo(new TimeSpan(0, 1, 0)));
        }

        [Test]
        public async Task ChainedGetRequestsWithUserAgent()
        {
            var tt = new TestTransport(HttpConfig.Default.WithUserAgent("Spider/1.0"))
                .Enqueue(new byte[0])
                .Enqueue(new byte[0]);

            await tt.Http.Get(new Uri("https://www.example.com/"))
                         .Get(new Uri("https://www.example.com/page"));

            var request1 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request1.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request1.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/page")));
            Assert.That(request1.Config.UserAgent, Is.EqualTo("Spider/1.0"));
            Assert.That(request2.Config.UserAgent, Is.EqualTo("Spider/1.0"));
        }

        [Test]
        public async Task SeparateRequestsWithCookie()
        {
            var cookie = new Cookie("name", "value");
            var tt = new TestTransport(HttpConfig.Default.WithCookies(new[] { cookie }))
                .Enqueue(new byte[0])
                .Enqueue(new byte[0]);

            await tt.Http.Get(new Uri("https://www.example.com"));
            await tt.Http.Get(new Uri("https://www.example.com/page"));

            var request1 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request1.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request1.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/page")));
            Assert.That(request1.Config.Cookies.Single(), Is.SameAs(cookie));
            Assert.That(request2.Config.Cookies.Single(), Is.SameAs(cookie));
        }

        [Test]
        public async Task SeparateRequestsWithCredentials()
        {
            var credentials = new NetworkCredential("admin", "admin");
            var tt = new TestTransport(HttpConfig.Default.WithCredentials(credentials))
                .Enqueue(new byte[0])
                .Enqueue(new byte[0]);

            await tt.Http.Get(new Uri("https://www.example.com"));
            await tt.Http.Get(new Uri("https://www.example.com/page"));

            var request1 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request1.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request1.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/page")));
            Assert.That(request1.Config.Credentials, Is.SameAs(credentials));
            Assert.That(request2.Config.Credentials, Is.SameAs(credentials));
        }

        [Test]
        public async Task SeparateRequestsWithHeader()
        {
            var tt = new TestTransport(HttpConfig.Default.WithHeader("name","value"))
                .Enqueue(new byte[0])
                .Enqueue(new byte[0]);

            await tt.Http.Get(new Uri("https://www.example.com"));
            await tt.Http.Get(new Uri("https://www.example.com/page"));

            var request1 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request1.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request1.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/page")));
            Assert.That(request1.Config.Headers.Single().Key, Is.EqualTo("name"));
            Assert.That(request2.Config.Headers.Single().Value.Single(), Is.EqualTo("value"));
        }

        [Test]
        public async Task SeparateRequestsWithTimeout()
        {
            var tt = new TestTransport(HttpConfig.Default.WithTimeout(new TimeSpan(0,1,0)))
                .Enqueue(new byte[0])
                .Enqueue(new byte[0]);

            await tt.Http.Get(new Uri("https://www.example.com"));
            await tt.Http.Get(new Uri("https://www.example.com/page"));

            var request1 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request1.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request1.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/page")));
            Assert.That(request1.Config.Timeout, Is.EqualTo(new TimeSpan(0, 1, 0)));
            Assert.That(request2.Config.Timeout, Is.EqualTo(new TimeSpan(0, 1, 0)));
        }

        [Test]
        public async Task PostRequestTest()
        {
            var tt = new TestTransport()
                .Enqueue(new byte[0]);
            var data = new NameValueCollection { ["name"] = "value" };

            await tt.Http.Post(new Uri("https://www.example.com/"), data);
            var request = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request.Config.Headers, Is.Empty);
            Assert.That(await request.Message.Content.ReadAsStringAsync(), Is.EqualTo("name=value"));
        }

        [Test]
        public async Task PostRequestWithTimeout()
        {
            var tt = new TestTransport(HttpConfig.Default.WithTimeout(new TimeSpan(0, 1, 0)))
                .Enqueue(new byte[0]);
            var data = new NameValueCollection { ["name"] = "value" };

            await tt.Http.Post(new Uri("https://www.example.com/"), data);
            var request = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request.Config.Timeout, Is.EqualTo(new TimeSpan(0, 1, 0)));
        }

        [Test]
        public async Task PostRequestWithUserAgent()
        {
            var tt = new TestTransport(HttpConfig.Default.WithUserAgent("Spider/1.0"))
                .Enqueue(new byte[0]);
            var data = new NameValueCollection { ["name"] = "value" };

            await tt.Http.Post(new Uri("https://www.example.com/"), data);
            var request = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request.Config.UserAgent, Is.EqualTo("Spider/1.0"));
        }

        [Test]
        public async Task PostRequestWithCookie()
        {
            var cookie = new Cookie("name", "value");
            var tt = new TestTransport(HttpConfig.Default.WithCookies(new[] { cookie }))
                .Enqueue(new byte[0]);
            var data = new NameValueCollection { ["name"] = "value" };

            await tt.Http.Post(new Uri("https://www.example.com/"), data);
            var request = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(await request.Message.Content.ReadAsStringAsync(), Is.EqualTo("name=value"));
            Assert.That(request.Config.Cookies.Single(), Is.SameAs(cookie));
        }

        [Test]
        public async Task PostRequestWithCredentials()
        {
            var credentials = new NetworkCredential("admin", "admin");
            var tt = new TestTransport(HttpConfig.Default.WithCredentials(credentials))
                .Enqueue(new byte[0]);
            var data = new NameValueCollection { ["name"] = "value" };

            await tt.Http.Post(new Uri("https://www.example.com/"), data);
            var request = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(await request.Message.Content.ReadAsStringAsync(), Is.EqualTo("name=value"));
            Assert.That(request.Config.Credentials, Is.SameAs(credentials));
        }
        [Test, Ignore("https://github.com/weblinq/WebLinq/issues/18")]
        public async Task ChainedPostRequestsWithDifferentHeaders()
        {
            var tt = new TestTransport()
                .Enqueue(new byte[0])
                .Enqueue(new byte[0]);
            var data1 = new NameValueCollection { [""] = "" };
            var data2 = new NameValueCollection { [""] = "" };

            await tt.Http.SetHeader("h1","v1")
                         .Post(new Uri("https://www.example.com/"), data1)
                         .SetHeader("h2", "v2")
                         .Post(new Uri("https://www.google.com/"), data2);
            var request1 = tt.DequeueRequest((m, c) => new { Message = m, c.Headers });
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, c.Headers });

            Assert.That(request1.Message.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request1.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.google.com/")));
            Assert.That(request1.Headers.Single().Key, Is.EqualTo("h1"));
            Assert.That(request2.Headers.Keys, Is.EquivalentTo(new[] { "h1", "h2" }));
            Assert.That(request1.Headers.Single().Value.Single(), Is.EqualTo("v1"));
            Assert.That(request2.Headers.Values.Select(v => v.Single()), Is.EquivalentTo(new[] { "v1", "v2" }));
        }


        [Test]
        public async Task ChainedPostRequests()
        {
            var tt = new TestTransport()
                .Enqueue(new byte[0])
                .Enqueue(new byte[0]);
            var data1 = new NameValueCollection { ["name1"] = "value1" };
            var data2 = new NameValueCollection { ["name2"] = "value2" };

            await tt.Http.Post(new Uri("https://www.example.com/"), data1)
                .Post(new Uri("https://www.google.com/"), data2);
            var message1 = tt.DequeueRequestMessage();
            var message2 = tt.DequeueRequestMessage();

            Assert.That(message1.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(message1.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(message2.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(message2.RequestUri, Is.EqualTo(new Uri("https://www.google.com/")));
            Assert.That(await message1.Content.ReadAsStringAsync(), Is.EqualTo("name1=value1"));
            Assert.That(await message2.Content.ReadAsStringAsync(), Is.EqualTo("name2=value2"));
        }

        [Test]
        public async Task ChainedPostRequestsWithCookie()
        {
            var cookie = new Cookie("name", "value");
            var tt = new TestTransport(HttpConfig.Default.WithCookies(new[] { cookie }))
                .Enqueue(new byte[0])
                .Enqueue(new byte[0]);
            var data1 = new NameValueCollection { [""] = "" };
            var data2 = new NameValueCollection { [""] = "" };

            await tt.Http.Post(new Uri("https://www.example.com/"), data1)
                .Post(new Uri("https://www.google.com/"), data2);

            var request1 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request1.Message.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request1.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.google.com/")));
            Assert.That(request1.Config.Cookies.Single(), Is.SameAs(cookie));
            Assert.That(request2.Config.Cookies.Single(), Is.SameAs(cookie));
        }

        [Test]
        public async Task ChainedPostRequestsWithCredentials()
        {
            var credentials = new NetworkCredential("admin", "admin");
            var tt = new TestTransport(HttpConfig.Default.WithCredentials(credentials))
                .Enqueue(new byte[0])
                .Enqueue(new byte[0]);

            var data1 = new NameValueCollection { [""] = "" };
            var data2 = new NameValueCollection { [""] = "" };

            await tt.Http.Post(new Uri("https://www.example.com/"), data1)
                .Post(new Uri("https://www.google.com/"), data2);

            var request1 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request1.Message.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request1.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.google.com/")));
            Assert.That(request1.Config.Credentials, Is.SameAs(credentials));
            Assert.That(request2.Config.Credentials, Is.SameAs(credentials));
        }

        [Test]
        public async Task ChainedPostRequestsWithTimeout()
        {
            var tt = new TestTransport(HttpConfig.Default.WithTimeout(new TimeSpan(0, 1, 0)))
                .Enqueue(new byte[0])
                .Enqueue(new byte[0]);
            var data1 = new NameValueCollection { [""] = "" };
            var data2 = new NameValueCollection { [""] = "" };

            await tt.Http.Post(new Uri("https://www.example.com/"), data1)
                 .Post(new Uri("https://www.google.com/"), data2);

            var request1 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request1.Message.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request1.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.google.com/")));
            Assert.That(request1.Config.Timeout, Is.EqualTo(new TimeSpan(0, 1, 0)));
            Assert.That(request2.Config.Timeout, Is.EqualTo(new TimeSpan(0, 1, 0)));
        }

        [Test]
        public async Task ChainedPostRequestsWithUserAgent()
        {
            var tt = new TestTransport(HttpConfig.Default.WithTimeout(new TimeSpan(0, 1, 0)))
                .Enqueue(new byte[0])
                .Enqueue(new byte[0]);
            var data1 = new NameValueCollection { [""] = "" };
            var data2 = new NameValueCollection { [""] = "" };

            await tt.Http.Post(new Uri("https://www.example.com/"), data1)
                 .Post(new Uri("https://www.google.com/"), data2);

            var request1 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request1.Message.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request1.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.google.com/")));
            Assert.That(request1.Config.Timeout, Is.EqualTo(new TimeSpan(0, 1, 0)));
            Assert.That(request2.Config.Timeout, Is.EqualTo(new TimeSpan(0, 1, 0)));
        }

        [Test]
        public async Task PostRequestWithHeader()
        {
            var tt = new TestTransport(HttpConfig.Default.WithHeader("foo", "bar"))
                .Enqueue(new byte[0]);
            var data = new NameValueCollection { ["name"] = "value" };

            await tt.Http.Post(new Uri("https://www.example.com/"), data);
            var request = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request.Config.Headers.Single().Key, Is.EqualTo("foo"));
            Assert.That(request.Config.Headers.Single().Value.Single(), Is.EqualTo("bar"));
        }

        [Test]
        public async Task PostRequestWith2NameValuePairs()
        {
            var tt = new TestTransport().Enqueue(new byte[0]);

            var data = new NameValueCollection
            {
                ["name1"] = "value1",
                ["name2"] = "value2",
            };

            await tt.Http.Post(new Uri("https://www.example.com/"), data);
            var message = tt.DequeueRequestMessage();

            Assert.That(message.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(await message.Content.ReadAsStringAsync(), Is.EqualTo("name1=value1&name2=value2"));
        }

        [Test]
        public async Task PostRequestWithEmptyNameValuePair()
        {
            var tt = new TestTransport().Enqueue(new byte[0]);

            var data = new NameValueCollection
            {
                [""] = "",
            };

            await tt.Http.Post(new Uri("https://www.example.com/"), data);
            var message = tt.DequeueRequestMessage();

            Assert.That(message.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(await message.Content.ReadAsStringAsync(), Is.EqualTo("="));
        }

        [Test]
        public async Task PostRequestWithAmpersandInNameValuePair()
        {
            var tt = new TestTransport().Enqueue(new byte[0]);

            var data = new NameValueCollection { ["foo&bar"] = "baz" };

            await tt.Http.Post(new Uri("https://www.example.com/"), data);
            var message = tt.DequeueRequestMessage();

            Assert.That(message.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(await message.Content.ReadAsStringAsync(), Is.EqualTo("foo%26bar=baz"));
        }

        [Test]
        public async Task SubmitNoDataTest()
        {
            var tt = new TestTransport()
                .EnqueueHtml(@"
                    <!DOCTYPE html>
                    <html>
                    <body>
                      <h2> HTML Forms </h2>
                      <form action='action_page.php'>
                        <br><br>
                    </form>
                    </body>
                    </html>")
                .Enqueue(new byte[0]);

            await tt.Http.Get(new Uri("https://www.example.com/"))
                         .Submit(0, null);

            var message1 = tt.DequeueRequestMessage();
            var message2 = tt.DequeueRequestMessage();

            Assert.That(message1.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(message1.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(message2.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(message2.RequestUri, Is.EqualTo(new Uri("https://www.example.com/action_page.php")));
        }

        [Test]
        public async Task SubmitPostNoDataTest()
        {
            var tt = new TestTransport()
                .EnqueueHtml(@"
                    <!DOCTYPE html>
                    <html>
                    <body>
                      <h2> HTML Forms </h2>
                      <form method='post' action='action_page.php'>
                        <br><br>
                    </form>
                    </body>
                    </html>")
                .Enqueue(new byte[0]);

            await tt.Http.Get(new Uri("https://www.example.com/"))
                         .Submit(0, null);

            var message1 = tt.DequeueRequestMessage();
            var message2 = tt.DequeueRequestMessage();

            Assert.That(message1.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(message1.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(message2.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(message2.RequestUri, Is.EqualTo(new Uri("https://www.example.com/action_page.php")));
        }

        [Test]
        public void PostNullDataTest()
        {
            var tt = new TestTransport()
                .Enqueue(new byte[0]);

            Assert.ThrowsAsync<NullReferenceException>(() => tt.Http.Post(new Uri("https://www.example.com/"), data:null).ToTask());
        }

        [Test]
        public async Task SubmitDefaultMethodIsGetTest()
        {
            var tt = new TestTransport()
                .EnqueueHtml(@"
                    <!DOCTYPE html>
                    <html>
                    <body>
                      <h2> HTML Forms </h2>
                      <form action='action_page.php'>
                        <br><br>
                    </form>
                    </body>
                    </html>")
                .Enqueue(new byte[0]);

            var data = new NameValueCollection
            {
                ["firstname"] = "Mickey",
                ["lastname"] = "Mouse"
            };

            await tt.Http.Get(new Uri("https://www.example.com/"))
                                 .Submit(0, data);

            var message1 = tt.DequeueRequestMessage();
            var message2 = tt.DequeueRequestMessage();

            Assert.That(message1.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(message1.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(message2.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(message2.RequestUri, Is.EqualTo(new Uri("https://www.example.com/action_page.php?firstname=Mickey&lastname=Mouse")));
        }

        [Test]
        public async Task SubmitInputTest()
        {
            var tt = new TestTransport()
                .EnqueueHtml(@"
                    <!DOCTYPE html>
                    <html>
                    <body>
                      <h2> HTML Forms </h2>
                      <form action='action_page.php'>
                        First name:<br>
                        <input type='text' name='firstname' value='Mickey' >
                        <br >
                        Last name:<br>
                        <input type='text' name='lastname' value='Mouse' >
                        <br><br>
                        <input type='submit' value='Submit' >
                      </form>
                    </body>
                    </html>")
                .Enqueue(new byte[0]);

            await tt.Http.Get(new Uri("https://www.example.com/"))
                         .Submit(0, null);

            var message1 = tt.DequeueRequestMessage();
            var message2 = tt.DequeueRequestMessage();

            Assert.That(message1.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(message1.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(message2.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(message2.RequestUri, Is.EqualTo(new Uri("https://www.example.com/action_page.php?firstname=Mickey&lastname=Mouse")));
        }

        [Test]
        public async Task SubmitWithCookie()
        {
            var cookie = new Cookie("name", "value");
            var tt = new TestTransport(HttpConfig.Default.WithCookies(new[] { cookie }))
                .EnqueueHtml(@"
                    <!DOCTYPE html>
                    <html>
                    <body>
                      <h2> HTML Forms </h2>
                      <form action='action_page.php'>
                        First name:<br>
                        <input type='text' name='firstname' value='Mickey' >
                        <br >
                        Last name:<br>
                        <input type='text' name='lastname' value='Mouse' >
                        <br><br>
                        <input type='submit' value='Submit' >
                      </form>
                    </body>
                    </html>")
                .Enqueue(new byte[0]);

            await tt.Http.Get(new Uri("https://www.example.com/"))
                         .Submit(0, null);

            var message1 = tt.DequeueRequestMessage();
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(message1.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(message1.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/action_page.php?firstname=Mickey&lastname=Mouse")));
            Assert.That(request2.Config.Cookies.Single(), Is.SameAs(cookie));
        }

        [Test]
        public async Task SubmitWithCredentials()
        {
            var credentials = new NetworkCredential("name", "value");
            var tt = new TestTransport(HttpConfig.Default.WithCredentials(credentials))
                .EnqueueHtml(@"
                    <!DOCTYPE html>
                    <html>
                    <body>
                      <h2> HTML Forms </h2>
                      <form action='action_page.php'>
                        First name:<br>
                        <input type='text' name='firstname' value='Mickey' >
                        <br >
                        Last name:<br>
                        <input type='text' name='lastname' value='Mouse' >
                        <br><br>
                        <input type='submit' value='Submit' >
                      </form>
                    </body>
                    </html>")
                .Enqueue(new byte[0]);

            await tt.Http.Get(new Uri("https://www.example.com/"))
                         .Submit(0, null);

            var message1 = tt.DequeueRequestMessage();
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(message1.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(message1.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/action_page.php?firstname=Mickey&lastname=Mouse")));
            Assert.That(request2.Config.Credentials, Is.SameAs(credentials));
        }

        [Test]
        public async Task SubmitWithUserAgent()
        {
            var tt = new TestTransport(HttpConfig.Default.WithUserAgent("Spider/1.0"))
                .EnqueueHtml(@"
                    <!DOCTYPE html>
                    <html>
                    <body>
                      <h2> HTML Forms </h2>
                      <form action='action_page.php'>
                        First name:<br>
                        <input type='text' name='firstname' value='Mickey' >
                        <br >
                        Last name:<br>
                        <input type='text' name='lastname' value='Mouse' >
                        <br><br>
                        <input type='submit' value='Submit' >
                      </form>
                    </body>
                    </html>")
                .Enqueue(new byte[0]);

            await tt.Http.Get(new Uri("https://www.example.com/"))
                         .Submit(0, null);

            var message1 = tt.DequeueRequestMessage();
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(message1.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(message1.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/action_page.php?firstname=Mickey&lastname=Mouse")));
            Assert.That(request2.Config.UserAgent, Is.EqualTo("Spider/1.0"));
        }

        [Test]
        public async Task SubmitWithHeader()
        {
            var tt = new TestTransport(HttpConfig.Default.WithHeader("foo", "bar"))
                .EnqueueHtml(@"
                    <!DOCTYPE html>
                    <html>
                    <body>
                      <h2> HTML Forms </h2>
                      <form action='action_page.php'>
                        First name:<br>
                        <input type='text' name='firstname' value='Mickey' >
                        <br >
                        Last name:<br>
                        <input type='text' name='lastname' value='Mouse' >
                        <br><br>
                        <input type='submit' value='Submit' >
                      </form>
                    </body>
                    </html>")
                .Enqueue(new byte[0]);

            await tt.Http.Get(new Uri("https://www.example.com/"))
                         .Submit(0, null);

            var message1 = tt.DequeueRequestMessage();
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(message1.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(message1.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/action_page.php?firstname=Mickey&lastname=Mouse")));
            Assert.That(request2.Config.Headers.Single().Key, Is.EqualTo("foo"));
            Assert.That(request2.Config.Headers.Single().Value.Single(), Is.EqualTo("bar"));
        }

        [Test]
        public async Task SubmitWithTimeout()
        {
            var tt = new TestTransport(HttpConfig.Default.WithTimeout(new TimeSpan(0,1,0)))
                .EnqueueHtml(@"
                    <!DOCTYPE html>
                    <html>
                    <body>
                      <h2> HTML Forms </h2>
                      <form action='action_page.php'>
                        First name:<br>
                        <input type='text' name='firstname' value='Mickey' >
                        <br >
                        Last name:<br>
                        <input type='text' name='lastname' value='Mouse' >
                        <br><br>
                        <input type='submit' value='Submit' >
                      </form>
                    </body>
                    </html>")
                .Enqueue(new byte[0]);

            await tt.Http.Get(new Uri("https://www.example.com/"))
                         .Submit(0, null);

            var message1 = tt.DequeueRequestMessage();
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(message1.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(message1.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/action_page.php?firstname=Mickey&lastname=Mouse")));
            Assert.That(request2.Config.Timeout, Is.EqualTo(new TimeSpan(0, 1, 0)));
        }

        [Test]
        public async Task SubmitInputPostTest()
        {
            var tt = new TestTransport()
                .EnqueueHtml(@"
                    <!DOCTYPE html>
                    <html>
                    <body>
                      <h2> HTML Forms </h2>
                      <form method='post' action='action_page.php'>
                        First name:<br>
                        <input type='text' name='firstname' value='Mickey' >
                        <br >
                        Last name:<br>
                        <input type='text' name='lastname' value='Mouse' >
                        <br><br>
                        <input type='submit' value='Submit' >
                      </form>
                    </body>
                    </html>")
                .Enqueue(new byte[0]);

            await tt.Http.Get(new Uri("https://www.example.com/"))
                         .Submit(0, null);

            var message1 = tt.DequeueRequestMessage();
            var message2 = tt.DequeueRequestMessage();

            Assert.That(message1.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(message1.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(message2.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(message2.RequestUri, Is.EqualTo(new Uri("https://www.example.com/action_page.php")));
            Assert.That(await message2.Content.ReadAsStringAsync(), Is.EqualTo("firstname=Mickey&lastname=Mouse"));
            Assert.That(message2.Content.Headers.GetValues("Content-Type").Single(), Is.EqualTo("application/x-www-form-urlencoded"));
        }

        [Test]
        public async Task SubmitPostTest()
        {
            var tt = new TestTransport()
                .EnqueueHtml(@"
                    <!DOCTYPE html>
                    <html>
                    <body>
                      <h2> HTML Forms </h2>
                      <form method='post' action='action_page.php'>
                        <br><br>
                      </form>
                    </body>
                    </html>")
                .Enqueue(new byte[0]);

            var data = new NameValueCollection
            {
                ["firstname"] = "Mickey",
                ["lastname"] = "Mouse"
            };

            await tt.Http.Get(new Uri("https://www.example.com/"))
                         .Submit(0, data);

            var message1 = tt.DequeueRequestMessage();
            var message2 = tt.DequeueRequestMessage();

            Assert.That(message1.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(message1.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(message2.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(message2.RequestUri, Is.EqualTo(new Uri("https://www.example.com/action_page.php")));
            Assert.That(await message2.Content.ReadAsStringAsync(), Is.EqualTo("firstname=Mickey&lastname=Mouse"));
            Assert.That(message2.Content.Headers.GetValues("Content-Type").Single(), Is.EqualTo("application/x-www-form-urlencoded"));
        }

        [Test]
        public async Task SubmitToInputTest()
        {
            var html = @"
                <!DOCTYPE html>
                <html>
                <body>
                  <h2> HTML Forms </h2>
                  <form action='action_page.php'>
                    First name:<br>
                    <input type='text' name='firstname' value='Mickey' >
                    <br >
                    Last name:<br>
                    <input type='text' name='lastname' value='Mouse' >
                    <br><br>
                    <input type='submit' value='Submit' >
                  </form>
                </body>
                </html>";

            var tt = new TestTransport().Enqueue(new byte[0]);

            await tt.Http.SubmitTo(new Uri("https://www.example.org/"),
                                   Html.HtmlParser.Default.Parse(html, new Uri("https://www.example.com/")),
                                   0, null);
            var message = tt.DequeueRequestMessage();

            Assert.That(message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(message.RequestUri, Is.EqualTo(new Uri("https://www.example.org/?firstname=Mickey&lastname=Mouse")));
            Assert.That(message.Headers, Is.Empty);
        }

        [Test]
        public async Task SubmitToNoInputTest()
        {
            var html = @"
                <!DOCTYPE html>
                <html>
                <body>
                  <h2> HTML Forms </h2>
                  <form action='action_page.php'>
                    <br><br>
                </form>
                </body>
                </html>";
            var tt = new TestTransport().Enqueue(new byte[0]);

            var data = new NameValueCollection
            {
                ["firstname"] = "Mickey",
                ["lastname"] = "Mouse"
            };
            await tt.Http.SubmitTo(new Uri("https://www.example.org/"),
                                   Html.HtmlParser.Default.Parse(html, new Uri("https://www.example.com/")),
                                   0, data);
            var message = tt.DequeueRequestMessage();

            Assert.That(message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(message.RequestUri, Is.EqualTo(new Uri("https://www.example.org/?firstname=Mickey&lastname=Mouse")));
            Assert.That(message.Headers, Is.Empty);
        }

        [Test]
        public async Task SubmitToPostRequestTest()
        {
            var html = @"
                <!DOCTYPE html>
                <html>
                <body>
                  <h2> HTML Forms </h2>
                  <form method='post' action='action_page.php'>
                    <br><br>
                </form>
                </body>
                </html>";

            var tt = new TestTransport().Enqueue(new byte[0]);

            var data = new NameValueCollection
            {
                ["firstname"] = "Mickey",
                ["lastname"] = "Mouse"
            };
            await tt.Http.SubmitTo(new Uri("https://www.example.org/"),
                                   Html.HtmlParser.Default.Parse(html, new Uri("https://www.example.com/")),
                                   0, data);
            var message = tt.DequeueRequestMessage();

            Assert.That(message.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(message.RequestUri, Is.EqualTo(new Uri("https://www.example.org/")));
            Assert.That(message.Content.Headers.GetValues("Content-Type").Single(), Is.EqualTo("application/x-www-form-urlencoded"));
            Assert.That(await message.Content.ReadAsStringAsync(), Is.EqualTo("firstname=Mickey&lastname=Mouse"));
        }

        [Test(Description = "https://github.com/weblinq/WebLinq/issues/19")]
        [TestCase(30_000)]
        [TestCase(33_000)]
        [TestCase(64_000)]
        [TestCase(65_000)]
        [TestCase(70_000)]
        public async Task PostLargeData(int size)
        {
            var tt = new TestTransport().Enqueue(new byte[0]);

            await tt.Http.Post(new Uri("https://www.example.com/"), new NameValueCollection
            {
                ["foo"] = new string('*', size)
            });

            Assert.Pass();
        }

        [TestCase(HttpStatusCode.Ambiguous)]
        [TestCase(HttpStatusCode.Moved)]
        [TestCase(HttpStatusCode.Redirect)]
        [TestCase(HttpStatusCode.RedirectMethod)]
        [TestCase(HttpStatusCode.RedirectKeepVerb)]
        public async Task RedirectionTests(HttpStatusCode statusCode)
        {
            var tt = new TestTransport()
                .Enqueue(new HttpResponseMessage
                {
                    Content = new ByteArrayContent(new byte[0]),
                    Headers =
                    {
                        Location =  new Uri("https://www.example.com/index.htm")
                    },
                    StatusCode = statusCode,
                })
                .Enqueue(new byte[0]);

            await tt.Http.Get(new Uri("https://www.example.com/"));
            tt.DequeueRequestMessage();
            var message = tt.DequeueRequestMessage();

            Assert.That(message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/index.htm")));
        }
    }
}
