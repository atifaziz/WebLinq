namespace WebLinq.Tests
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
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
            var tt = new TestTransport(
                new HttpResponseMessage
                {
                    Content = new StringContent(string.Empty, Encoding.UTF8, "text/plain")
                });

            await tt.Http.Get(new Uri("https://www.example.com"))
                      .Accept("text/plain");

            // Succeeds if it doesn't throw.
        }

        [Test]
        public async Task AcceptMultipleMediaTypes()
        {
            var tt = new TestTransport(
                new HttpResponseMessage
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                });

            await tt.Http.Get(new Uri("https://www.example.com"))
                      .Accept("text/json", "application/json");

            // Succeeds if it doesn't throw.
        }

        [Test]
        public void AcceptThrowsExceptionOnMediaTypeMismatch()
        {
            var tt = new TestTransport(
                new HttpResponseMessage
                {
                    Content = new StringContent("foo", Encoding.UTF8, "text/plain")
                });

            Assert.ThrowsAsync<Exception>(() =>
                tt.Http.Get(new Uri("https://www.example.com"))
                    .Accept("text/html")
                    .ToTask());
        }

        [Test]
        public async Task GetRequestTest()
        {
            var tt = new TestTransport(
                new HttpResponseMessage
                {
                    Content = new ByteArrayContent(new byte[0]),
                });

            await tt.Http.Get(new Uri("https://www.example.com"));
            var request = tt.DequeueRequestMessage();

            Assert.That(request.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));
            Assert.That(request.Headers, Is.Empty);
        }

        [Test]
        public async Task GetRequestSetHeaderTest()
        {
            var tt = new TestTransport(HttpConfig.Default.WithHeaders(HttpHeaderCollection.Empty), new HttpResponseMessage
            {
                Content = new ByteArrayContent(new byte[0]),
            });

            await tt.Http.SetHeader("foo", "bar").Get(new Uri("https://www.example.com"));
            var request = tt.DequeueRequest((m, c) => new { Message = m, c.Headers });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));

            Assert.That(tt.Http.Config.Headers, Is.Empty);
            Assert.That(request.Headers.Single().Key, Is.EqualTo("foo"));
            Assert.That(request.Headers.Single().Value.Single(), Is.EqualTo("bar"));
        }

        [Test]
        public void NotFoundFetchThrowsException()
        {
            var tt = new TestTransport(
                new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Content = new ByteArrayContent(new byte[0]),
                });

            Assert.Throws<HttpRequestException>(() => tt.Http.Get(new Uri("https://www.example.com")).GetAwaiter().GetResult());
        }

        [Test]
        public async Task NotFoundFetchReturnErroneousFetchTest()
        {
            var tt = new TestTransport(
                new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Content = new ByteArrayContent(new byte[0]),
                });

            var result = await tt.Http.Get(new Uri("https://www.example.com"))
                                   .ReturnErrorneousFetch();

            Assert.That(result.RequestUrl, Is.EqualTo(new Uri("https://www.example.com")));
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public void NotImplementedErroneousFetchThrowsException()
        {
            var tt = new TestTransport(
                new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotImplemented,
                    Content = new ByteArrayContent(new byte[0]),
                });

            Assert.Throws<HttpRequestException>(() => tt.Http.Get(new Uri("https://www.example.com")).GetAwaiter().GetResult());
        }

        [Test]
        public async Task NotImplementedFetchReturnErroneousFetchTest()
        {
            var tt = new TestTransport(
                new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotImplemented,
                    Content = new ByteArrayContent(new byte[0]),
                });

            var result = await tt.Http.Get(new Uri("https://www.example.com"))
                                   .ReturnErrorneousFetch();

            Assert.That(result.RequestUrl, Is.EqualTo(new Uri("https://www.example.com")));
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.NotImplemented));
        }

        [Test]
        public void BadGatewayFetchThrowsException()
        {
            var tt = new TestTransport(
                new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadGateway,
                    Content = new ByteArrayContent(new byte[0]),
                });

            Assert.Throws<HttpRequestException>(() => tt.Http.Get(new Uri("https://www.example.com")).GetAwaiter().GetResult());
        }

        [Test]
        public async Task BadGatewayFetchReturnErroneousFetchTest()
        {
            var tt = new TestTransport(
                new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadGateway,
                    Content = new ByteArrayContent(new byte[0]),
                });

            var result = await tt.Http.Get(new Uri("https://www.example.com"))
                                   .ReturnErrorneousFetch();

            Assert.That(result.RequestUrl, Is.EqualTo(new Uri("https://www.example.com")));
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.BadGateway));
        }

        [Test]
        public void GatewayTimeoutFetchThrowsException()
        {
            var tt = new TestTransport(
                new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.GatewayTimeout,
                    Content = new ByteArrayContent(new byte[0]),
                });

            Assert.Throws<HttpRequestException>(() => tt.Http.Get(new Uri("https://www.example.com")).GetAwaiter().GetResult());
        }

        [Test]
        public async Task GatewayTimeoutFetchReturnErroneousFetchTest()
        {
            var tt = new TestTransport(
                new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.GatewayTimeout,
                    Content = new ByteArrayContent(new byte[0]),
                });

            var result = await tt.Http.Get(new Uri("https://www.example.com"))
                                   .ReturnErrorneousFetch();

            Assert.That(result.RequestUrl, Is.EqualTo(new Uri("https://www.example.com")));
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.GatewayTimeout));
        }

        [Test]
        public void ForbiddenFetchThrowsException()
        {
            var tt = new TestTransport(
                new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Forbidden,
                    Content = new ByteArrayContent(new byte[0]),
                });

            Assert.Throws<HttpRequestException>(() => tt.Http.Get(new Uri("https://www.example.com")).GetAwaiter().GetResult());
        }

        [Test]
        public async Task ForbiddenFetchReturnErroneousFetchTest()
        {
            var tt = new TestTransport(
                new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Forbidden,
                    Content = new ByteArrayContent(new byte[0]),
                });

            var result = await tt.Http.Get(new Uri("https://www.example.com"))
                                   .ReturnErrorneousFetch();

            Assert.That(result.RequestUrl, Is.EqualTo(new Uri("https://www.example.com")));
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        }

        [Test]
        public async Task SetCookieHeaderTest()
        {
            var tt = new TestTransport(new HttpResponseMessage
            {
                Content = new ByteArrayContent(new byte[0]),
            }, new HttpResponseMessage
            {
                Headers =
                {
                    { "Set-Cookie", "foo=bar" }
                },
                Content = new ByteArrayContent(new byte[0]),
            });

            var result = await tt.Http.Get(new Uri("https://www.example.com"))
                                   .Get(new Uri("https://www.example.com/page"));

            var request1 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request1.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request1.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/page")));
            Assert.That(result.Client.Config.Cookies.Single().Name, Is.EqualTo("foo"));
            Assert.That(result.Client.Config.Cookies.Single().Value, Is.EqualTo("bar"));
        }


        [Test]
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

            var result = await tt.Http.Get(new Uri("https://www.example.com"))
                                   .Get(new Uri("https://www.google.com"));
            var request1 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request1.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request1.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.google.com")));
            Assert.That(result.Client.Config.Cookies.Single(c => c.Domain.Equals("www.example.com")).Name, Is.EqualTo("foo"));
            Assert.That(result.Client.Config.Cookies.Single(c => c.Domain.Equals("www.example.com")).Value, Is.EqualTo("bar"));
            Assert.That(result.Client.Config.Cookies.Single(c => c.Domain.Equals("www.google.com")).Name, Is.EqualTo("foo"));
            Assert.That(result.Client.Config.Cookies.Single(c => c.Domain.Equals("www.google.com")).Value, Is.EqualTo("bar"));
        }

        [Test]
        public async Task CookiesKeptInSubdomainWhenSpecified()
        {
            var tt = new TestTransport(
                new HttpResponseMessage
                {
                    Content = new ByteArrayContent(new byte[0]),
                },
                new HttpResponseMessage
                {
                    Headers =
                    {
                        { "Set-Cookie", "foo=bar;Domain=example.com" }
                    },
                    Content = new ByteArrayContent(new byte[0]),
                });

            var result = await
                tt.Http.Get(new Uri("https://www.example.com"))
                               .Get(new Uri("https://mail.example.com"));

            var request1 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request1.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request1.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://mail.example.com")));
            Assert.That(result.Client.Config.Cookies.Single().Name, Is.EqualTo("foo"));
            Assert.That(result.Client.Config.Cookies.Single().Value, Is.EqualTo("bar"));
        }

        [Test]
        public async Task WithHeaderGetRequestSetHeaderTest()
        {
            var tt = new TestTransport(HttpConfig.Default.WithHeader("name1","value1"),
                new HttpResponseMessage
                {
                    Content = new ByteArrayContent(new byte[0]),
                });

            await tt.Http.SetHeader("name2", "value2").Get(new Uri("https://www.example.com"));
            var request = tt.DequeueRequest((m, c) => new { Message = m, c.Headers });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));

            Assert.That(tt.Http.Config.Headers.Single().Key, Is.EqualTo("name1"));
            Assert.That(tt.Http.Config.Headers.Single().Value.Single(), Is.EqualTo("value1"));
            Assert.That(request.Headers.Keys, Is.EquivalentTo(new[] { "name1", "name2" }));
            Assert.That(request.Headers.Values.Select(v => v.Single()), Is.EquivalentTo(new[] { "value1","value2"}));
        }

        [Test]
        public async Task GetRequestsSetSameHeaderTest()
        {
            var tt = new TestTransport(HttpConfig.Default.WithHeader("foo", "bar"),
                new HttpResponseMessage
                {
                    Content = new ByteArrayContent(new byte[0]),
                });

            await tt.Http.SetHeader("foo", "bar").Get(new Uri("https://www.example.com"));
            var request = tt.DequeueRequest((m, c) => new { Message = m, c.Headers });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));

            Assert.That(request.Headers.Single().Key, Is.EqualTo("foo"));
            Assert.That(request.Headers.Single().Value.Single(), Is.EqualTo("bar"));
        }

        [Test]
        public async Task ChainedGetRequests()
        {
            var tt = new TestTransport(HttpConfig.Default,
                new HttpResponseMessage
                {
                    Content = new ByteArrayContent(new byte[0]),
                },
                new HttpResponseMessage
                {
                    Content = new ByteArrayContent(new byte[0]),
                });

            await tt.Http.Get(new Uri("https://www.example.com"))
                         .Get(new Uri("https://www.example.com/page"));
            var request1 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request1.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request1.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/page")));
            Assert.That(request1.Config, Is.SameAs(HttpConfig.Default));
            Assert.That(request2.Config, Is.SameAs(HttpConfig.Default));
        }

        [Test, Ignore("https://github.com/weblinq/WebLinq/issues/18")]
        public async Task ChainedRequestsWithDifferentHeaders()
        {
            var tt = new TestTransport(new HttpResponseMessage
            {
                Content = new ByteArrayContent(new byte[0]),
            }, new HttpResponseMessage
            {
                Content = new ByteArrayContent(new byte[0]),
            });

            await tt.Http.SetHeader("h1", "v1")
                         .Get(new Uri("https://www.example.com"))
                         .SetHeader("h2", "v2")
                         .Get(new Uri("https://www.example.com/page"));

            var request1 = tt.DequeueRequest((m, c) => new { Message = m, c.Headers });
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, c.Headers });

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
            var tt = new TestTransport(new HttpResponseMessage
            {
                Content = new ByteArrayContent(new byte[0]),
            }, new HttpResponseMessage
            {
                Content = new ByteArrayContent(new byte[0]),
            });

            await tt.Http.SetHeader("h1", "v1").Get(new Uri("https://www.example.com"));
            await tt.Http.SetHeader("h2", "v2").Get(new Uri("https://www.example.com/page"));

            var request1 = tt.DequeueRequest((m, c) => new { Message = m, c.Headers });
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, c.Headers });

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
            var tt = new TestTransport(HttpConfig.Default.WithHeader("foo", "bar"),
                new HttpResponseMessage
                {
                    Content = new ByteArrayContent(new byte[0]),
                });

            await tt.Http.Get(new Uri("https://www.example.com"));
            var request = tt.DequeueRequest((m, c) => new { Message = m, Config = c});

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));
            Assert.That(request.Config.Headers.Single().Key, Is.EqualTo("foo"));
            Assert.That(request.Config.Headers.Single().Value.Single(), Is.EqualTo("bar"));
        }

        [Test]
        public async Task GetRequestWithUserAgentTest()
        {
            var tt = new TestTransport(HttpConfig.Default.WithUserAgent("Spider/1.0"),
                new HttpResponseMessage
                {
                    Content = new ByteArrayContent(new byte[0]),
                });

            await tt.Http.Get(new Uri("https://www.example.com"));
            var request = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));
            Assert.That(request.Config.UserAgent, Is.EqualTo("Spider/1.0"));
        }

        [Test]
        public async Task GetRequestWithCredentialsTest()
        {
            var credentials = new NetworkCredential("admin", "admin");
            var tt = new TestTransport(HttpConfig.Default.WithCredentials(credentials),
                new HttpResponseMessage
                {
                    Content = new ByteArrayContent(new byte[0])
                });

            await tt.Http.Get(new Uri("https://www.example.com"));
            var request = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));
            Assert.That(request.Config.Credentials, Is.SameAs(credentials));
        }

        [Test]
        public async Task GetRequestWithTimeout()
        {
            var tt = new TestTransport(HttpConfig.Default.WithTimeout(new TimeSpan(0, 1, 0)),
                new HttpResponseMessage
                {
                    Content = new ByteArrayContent(new byte[0]),
                });

            await tt.Http.Get(new Uri("https://www.example.com"));
            var request = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));
            Assert.That(request.Config.Timeout, Is.EqualTo(new TimeSpan(0, 1, 0)));
        }

        [Test]
        public async Task GetRequestWithCookies()
        {
            var tt = new TestTransport(HttpConfig.Default.WithCookies(new[] { new Cookie("name", "value") }),
                new HttpResponseMessage
                {
                    Content = new ByteArrayContent(new byte[0]),
                });

            await tt.Http.Get(new Uri("https://www.example.com"));
            var request = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));
            Assert.That(request.Config.Cookies.Single(), Is.EqualTo(new Cookie("name", "value")));
        }

        [Test]
        public async Task ChainedGetRequestsWithCookie()
        {
            var tt = new TestTransport(HttpConfig.Default.WithCookies(new[] { new Cookie("name", "value") }),
                new HttpResponseMessage
                {
                    Content = new ByteArrayContent(new byte[0]),
                },
                new HttpResponseMessage
                {
                    Content = new ByteArrayContent(new byte[0]),
                });

            await tt.Http.Get(new Uri("https://www.example.com"))
                         .Get(new Uri("https://www.example.com/page"));

            var request1 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

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
            var tt = new TestTransport(
                new HttpResponseMessage
                {
                    Content = new ByteArrayContent(new byte[0]),
                });

            var data = new NameValueCollection { ["name"] = "value" };

            await tt.Http.Post(new Uri("https://www.example.com"), data);
            var request = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));

            Assert.That(request.Config.Headers, Is.Empty);
            Assert.That(await request.Message.Content.ReadAsStringAsync(), Is.EqualTo("name=value"));
        }

        [Test]
        public async Task PostRequestWithCookie()
        {
            var tt = new TestTransport(HttpConfig.Default.WithCookies(new[] { new Cookie("name", "value") }),
                new HttpResponseMessage
                {
                    Content = new ByteArrayContent(new byte[0]),
                });

            var data = new NameValueCollection { ["name"] = "value" };

            await tt.Http.Post(new Uri("https://www.example.com"), data);
            var request = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));

            Assert.That(request.Config.Headers, Is.Empty);
            Assert.That(await request.Message.Content.ReadAsStringAsync(), Is.EqualTo("name=value"));
        }

        [Test]
        public async Task PostRequestWith2NameValuePairs()
        {
            var tt = new TestTransport(
                new HttpResponseMessage
                {
                    Content = new ByteArrayContent(new byte[0]),
                });

            var data = new NameValueCollection
            {
                ["name1"] = "value1",
                ["name2"] = "value2",
            };

            await tt.Http.Post(new Uri("https://www.example.com"), data);
            var request = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));

            Assert.That(request.Config.Headers, Is.Empty);
            Assert.That(await request.Message.Content.ReadAsStringAsync(), Is.EqualTo("name1=value1&name2=value2"));
        }

        [Test]
        public async Task PostRequestWithAmpersandInNameValuePair()
        {
            var tt = new TestTransport(
                new HttpResponseMessage
                {
                    Content = new ByteArrayContent(new byte[0]),
                });

            var data = new NameValueCollection { ["foo&bar"] = "baz" };

            await tt.Http.Post(new Uri("https://www.example.com"), data);
            var request = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));

            Assert.That(request.Config.Headers, Is.Empty);
            Assert.That(await request.Message.Content.ReadAsStringAsync(), Is.EqualTo("foo%26bar=baz"));
        }

        [Test]
        public async Task SubmitNoFormNoInputTest()
        {
            var action = "/action_page.php";
            var tt = new TestTransport(
                new HttpResponseMessage
                {
                    Content = new StringContent("<!DOCTYPE html>" +
                                                "<html>" +
                                                "<body>" +
                                                "<h2> HTML Forms </h2>" +
                                                "<form action=\"" + action + "\">" +
                                                "  <br><br>" +
                                                "  <input type=\"submit\" value=\"Submit\" >" +
                                                "</form>" +
                                                "</body>" +
                                                "</html>", Encoding.UTF8, "text/html"),
                },
                new HttpResponseMessage
                {
                    Content = new ByteArrayContent(new byte[0]),
                });

            await tt.Http.Get(new Uri("https://www.example.com"))
                         .Submit(0, null);

            var request1 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request1.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request1.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com" + action)));
        }

        [Test]
        public async Task SubmitDefaultMethodIsGetTest()
        {
            var action = "/action_page.php";
            var tt = new TestTransport(
                new HttpResponseMessage
                {
                    Content = new StringContent("<!DOCTYPE html>" +
                                                "<html>" +
                                                "<body>" +
                                                "<h2> HTML Forms </h2>" +
                                                "<form action=\"" + action + "\">" +
                                                "  <br><br>" +
                                                "  <input type=\"submit\" value=\"Submit\" >" +
                                                "</form>" +
                                                "</body>" +
                                                "</html>", Encoding.UTF8, "text/html"),
                },
                new HttpResponseMessage
                {
                    Content = new ByteArrayContent(new byte[0]),
                });

            var data = new NameValueCollection
            {
                ["firstname"] = "Mickey",
                ["lastname"] = "Mouse"
            };

            await tt.Http.Get(new Uri("https://www.example.com"))
                                 .Submit(0, data);

            var request1 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request1.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request1.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com" + action + "?firstname=Mickey&lastname=Mouse")));
        }

        [Test]
        public async Task SubmitInputTest()
        {
            var action = "/action_page.php";
            var tt = new TestTransport(
                new HttpResponseMessage
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
                new HttpResponseMessage
                {
                    Content = new ByteArrayContent(new byte[0]),
                });

            await tt.Http.Get(new Uri("https://www.example.com"))
                         .Submit(0, null);

            var request1 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request1.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request1.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com" + action + "?firstname=Mickey&lastname=Mouse")));
        }


        [Test]
        public async Task SubmitInputPostTest()
        {
            var action = "/action_page.php";
            var tt = new TestTransport(
                new HttpResponseMessage
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
                new HttpResponseMessage
                {
                    Content = new ByteArrayContent(new byte[0]),
                });

            await tt.Http.Get(new Uri("https://www.example.com"))
                         .Submit(0, null);

            var request1 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request1.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request1.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com" + action)));
            Assert.That(await request2.Message.Content.ReadAsStringAsync(), Is.EqualTo("firstname=Mickey&lastname=Mouse"));
            Assert.That(request2.Message.Content.Headers.GetValues("Content-Type").Single(), Is.EqualTo("application/x-www-form-urlencoded"));
        }

        [Test]
        public async Task SubmitPostTest()
        {
            var action = "/action_page.php";
            var tt = new TestTransport(
                new HttpResponseMessage
                {
                    Content = new StringContent("<!DOCTYPE html>" +
                                                "<html>" +
                                                "<body>" +
                                                "<h2> HTML Forms </h2>" +
                                                "<form method='post' action=\"" + action + "\">" +
                                                "  <br><br>" +
                                                "  <input type=\"submit\" value=\"Submit\" >" +
                                                "</form>" +
                                                "</body>" +
                                                "</html>", Encoding.UTF8, "text/html"),
                },
                new HttpResponseMessage
                {
                    Content = new ByteArrayContent(new byte[0]),
                });

            var data = new NameValueCollection
            {
                ["firstname"] = "Mickey",
                ["lastname"] = "Mouse"
            };

            await tt.Http.Get(new Uri("https://www.example.com"))
                         .Submit(0, data);

            var request1 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request1.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request1.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com" + action)));
            Assert.That(await request2.Message.Content.ReadAsStringAsync(), Is.EqualTo("firstname=Mickey&lastname=Mouse"));
            Assert.That(request2.Message.Content.Headers.GetValues("Content-Type").Single(), Is.EqualTo("application/x-www-form-urlencoded"));
        }

        [Test]
        public async Task SubmitToInputTest()
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
            var tt = new TestTransport(
                new HttpResponseMessage
                {
                    Content = new ByteArrayContent(new byte[0]),
                });

            await tt.Http.SubmitTo(new Uri("https://www.example.org"),
                                   Html.HtmlParser.Default.Parse(html, new Uri("https://www.example.com")),
                                   0, null);
            var request = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.org" + "?firstname=Mickey&lastname=Mouse")));
            Assert.That(request.Message.Headers, Is.Empty);
        }

        [Test]
        public async Task SubmitToNoInputTest()
        {
            var action = "/action_page.php";
            var html = "<!DOCTYPE html>" +
                                                 "<html>" +
                                                 "<body>" +
                                                 "<h2> HTML Forms </h2>" +
                                                 "<form action=\"" + action + "\">" +
                                                 "  <br><br>" +
                                                 "  <input type=\"submit\" value=\"Submit\" >" +
                                                 "</form>" +
                                                 "</body>" +
                                                 "</html>";
            var tt = new TestTransport(
                new HttpResponseMessage
                {
                    Content = new ByteArrayContent(new byte[0]),
                });
            var data = new NameValueCollection
            {
                ["firstname"] = "Mickey",
                ["lastname"] = "Mouse"
            };
            await tt.Http.SubmitTo(new Uri("https://www.example.org"),
                                   Html.HtmlParser.Default.Parse(html, new Uri("https://www.example.com")),
                                   0, data);
            var request = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

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
                       "  <br><br>" +
                       "  <input type=\"submit\" value=\"Submit\" >" +
                       "</form>" +
                       "</body>" +
                       "</html>";

            var tt = new TestTransport(
                new HttpResponseMessage
                {
                    Content = new ByteArrayContent(new byte[0]),
                });
            var data = new NameValueCollection
            {
                ["firstname"] = "Mickey",
                ["lastname"] = "Mouse"
            };
            await tt.Http.SubmitTo(new Uri("https://www.example.org"),
                                   Html.HtmlParser.Default.Parse(html, new Uri("https://www.example.com")),
                                   0, data);
            var request = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.org")));
            Assert.That(request.Message.Content.Headers.GetValues("Content-Type").Single(), Is.EqualTo("application/x-www-form-urlencoded"));
            Assert.That(await request.Message.Content.ReadAsStringAsync(), Is.EqualTo("firstname=Mickey&lastname=Mouse"));
            Assert.That(request.Message.Content.Headers.GetValues("Content-Type").Single(), Is.EqualTo("application/x-www-form-urlencoded"));
        }
    }
}
