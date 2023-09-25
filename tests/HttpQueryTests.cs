namespace WebLinq.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Collections.Specialized;
    using System.Net;
    using System.Net.Http.Headers;
    using NUnit.Framework;

    public class HttpQueryTests
    {
        static readonly byte[] ZeroBytes = Array.Empty<byte>();

        static Task<T> EvaluateAsync<T>(IHttpClient client, IHttpQuery<T> query) =>
            EvaluateAsync(client, query, static e => e.SingleAsync());

        static async Task<TResult> EvaluateAsync<T, TResult>(IHttpClient client, IHttpQuery<T> query,
                                                             Func<IAsyncEnumerable<T>, ValueTask<TResult>> selector)
        {
            using var context = new HttpQueryContext(client);
            return await selector(query.Share(context));
        }

        [Test]
        public async Task AcceptSingleMediaType()
        {
            var q = Http.Get(new Uri("https://www.example.com/"))
                        .Accept("text/plain");

            using var tt = new TestTransport().EnqueueText(string.Empty);
            _ = await EvaluateAsync(tt, q);

            Assert.Pass(); // Succeeds if it doesn't throw.
        }

        [Test]
        public async Task AcceptMultipleMediaTypes()
        {
            var q = Http.Get(new Uri("https://www.example.com/"))
                        .Accept("text/json", "application/json");

            using var tt = new TestTransport().EnqueueJson("{}");
            _ = await EvaluateAsync(tt, q);

            Assert.Pass(); // Succeeds if it doesn't throw.
        }

        [Test]
        public void AcceptThrowsExceptionOnMediaTypeMismatch()
        {
            var q = Http.Get(new Uri("https://www.example.com/"))
                        .Accept("text/html");

            using var tt = new TestTransport().EnqueueText("foo");
            Assert.ThrowsAsync<UnacceptableMediaException>(() => EvaluateAsync(tt, q));
        }

        [Test]
        public void AcceptThrowsExceptionOnEmptyString()
        {
            var q = Http.Get(new Uri("https://www.example.com/"))
                        .Accept("");

            using var tt = new TestTransport().Enqueue(ZeroBytes);
            Assert.ThrowsAsync<UnacceptableMediaException>(() => EvaluateAsync(tt, q));
        }

        [Test]
        public async Task AcceptNoParameterTest()
        {
            var q = Http.Get(new Uri("https://www.example.com/"))
                        .Accept();

            using var tt = new TestTransport().Enqueue(ZeroBytes);
            _ = await EvaluateAsync(tt, q);

            Assert.Pass(); // Succeeds if it doesn't throw.
        }

        [Test]
        public async Task GetRequestTest()
        {
            var q = Http.Get(new Uri("https://www.example.com/"));

            using var tt = new TestTransport().Enqueue(ZeroBytes);
            _ = await EvaluateAsync(tt, q);
            var request = tt.DequeueRequestMessage();

            Assert.That(request.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request.Headers, Is.Empty);
        }

        [Test]
        public async Task SetHeaderTest()
        {
            var q = Http.Get(new Uri("https://www.example.com/"))
                        .SetHeader("foo", "bar")
                        .SetHeader("bar", "baz")
                        .SetHeader("baz", "qux");

            using var tt = new TestTransport().Enqueue(ZeroBytes);
            _ = await EvaluateAsync(tt, q);
            var request = tt.DequeueRequestMessage();

            Assert.That(request.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request.Headers.Count(), Is.EqualTo(3));
            Assert.That(request.Headers.GetValues("foo"), Is.EqualTo(new[] { "bar" }));
            Assert.That(request.Headers.GetValues("bar"), Is.EqualTo(new[] { "baz" }));
            Assert.That(request.Headers.GetValues("baz"), Is.EqualTo(new[] { "qux" }));
        }

        [Test]
        public async Task SetHeaderOnFirstRequestTest()
        {
            var q = Http.Get(new Uri("https://www.example.com/"))
                        .SetHeader("foo", "bar")
                        .Get(new Uri("https://www.example.com/"));

            using var tt = new TestTransport().Enqueue(ZeroBytes).Enqueue(ZeroBytes);
            _ = await EvaluateAsync(tt, q);
            var request1 = tt.DequeueRequestMessage();
            var request2 = tt.DequeueRequestMessage();

            Assert.That(request1.Headers.Count(), Is.EqualTo(1));
            Assert.That(request1.Headers.GetValues("foo"), Is.EqualTo(new[] { "bar" }));
            Assert.That(request2.Headers, Is.Empty);
        }

        /* FIXME
        [Test]
        public async Task GetRequestSetHeaderTest()
        {
            var tt = new TestTransport(HttpConfig.Default.WithHeaders(HttpHeaderCollection.Empty));
            tt.Enqueue(ZeroBytes);

            var q = Http.SetHeader("foo", "bar").Get(new Uri("https://www.example.com/"));
            var request = tt.DequeueRequest((m, c) => new { Message = m, c.Headers });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(tt.Http.Config.Headers, Is.Empty);
            Assert.That(request.Headers.Single().Key, Is.EqualTo("foo"));
            Assert.That(request.Headers.Single().Value.Single(), Is.EqualTo("bar"));
        }
        */

        [TestCase(HttpStatusCode.NotFound)]
        [TestCase(HttpStatusCode.NotImplemented)]
        [TestCase(HttpStatusCode.BadGateway)]
        [TestCase(HttpStatusCode.GatewayTimeout)]
        [TestCase(HttpStatusCode.Forbidden)]
        public void NotFoundFetchThrowsException(HttpStatusCode statusCode)
        {
            var q = Http.Get(new Uri("https://www.example.com/"));

            using var tt = new TestTransport().Enqueue(ZeroBytes, statusCode);
            Assert.ThrowsAsync<HttpRequestException>(() => EvaluateAsync(tt, q));
        }

        [TestCase(HttpStatusCode.NotFound)]
        [TestCase(HttpStatusCode.NotImplemented)]
        [TestCase(HttpStatusCode.BadGateway)]
        [TestCase(HttpStatusCode.GatewayTimeout)]
        [TestCase(HttpStatusCode.Forbidden)]
        public async Task NotFoundFetchReturnErroneousFetchTest(HttpStatusCode statusCode)
        {
            var q = Http.Get(new Uri("https://www.example.com/"))
                        .ReturnErroneousFetch();

            using var tt = new TestTransport().Enqueue(ZeroBytes, statusCode);
            var result = await EvaluateAsync(tt, q);

            Assert.That(result.RequestUrl, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(result.StatusCode, Is.EqualTo(statusCode));
        }

        /*
        [Test]
        public async Task SetCookieHeaderInResponseTest()
        {
            var q = Http.Get(new Uri("https://www.example.com/"))
                        .Get(new Uri("https://www.example.com/page"));

            using var response = new HttpResponseMessage
            {
                Headers =
                {
                    { "Set-Cookie", "foo=bar" }
                },
                Content = new ByteArrayContent(ZeroBytes),
            };

            using var tt = new TestTransport()
                .Enqueue(ZeroBytes)
                .Enqueue(response);

            var result = await EvaluateAsync(tt, q);

            var request1 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request1.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request1.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/page")));
            Assert.That(result.Cookies.Single().Name, Is.EqualTo("foo"));
            Assert.That(result.Cookies.Single().Value, Is.EqualTo("bar"));
        }

        [Test]
        public async Task WithHeaderGetRequestSetHeaderTest()
        {
            var tt = new TestTransport(HttpConfig.Default.WithHeader("name1", "value1"))
                .Enqueue(ZeroBytes);

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
                .Enqueue(ZeroBytes);

            await tt.Http.SetHeader("foo", "bar").Get(new Uri("https://www.example.com/"));
            var request = tt.DequeueRequest((m, c) => new { Message = m, c.Headers });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request.Headers.Single().Key, Is.EqualTo("foo"));
            Assert.That(request.Headers.Single().Value.Single(), Is.EqualTo("bar"));
        }
        */

        [Test]
        public async Task ChainedGetRequests()
        {
            var q = Http.Get(new Uri("https://www.example.com/"))
                        .Get(new Uri("https://www.example.com/page"));

            using var tt = new TestTransport()
                .Enqueue(ZeroBytes)
                .Enqueue(ZeroBytes);

            _ = await EvaluateAsync(tt, q);

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
            var q = Http.SetHeader("h1", "v1")
                        .Get(new Uri("https://www.example.com/"))
                        .SetHeader("h2", "v2")
                        .Get(new Uri("https://www.example.com/page"));

            using var tt = new TestTransport()
                     .Enqueue(ZeroBytes)
                     .Enqueue(ZeroBytes);

            await EvaluateAsync(tt, q);

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
            var q1 = Http.SetHeader("h1", "v1").Get(new Uri("https://www.example.com/"));
            var q2 = Http.SetHeader("h2", "v2").Get(new Uri("https://www.example.com/page"));

            using var tt = new TestTransport()
                     .Enqueue(ZeroBytes)
                     .Enqueue(ZeroBytes);

            await EvaluateAsync(tt, q1);
            await EvaluateAsync(tt, q2);

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

        [Test]
        public async Task GetRequestWithHeader()
        {
            var q = Http.SetHeader("foo", "bar")
                        .Get(new Uri("https://www.example.com/"));

            using var tt = new TestTransport()
                .Enqueue(ZeroBytes);

            _ = await EvaluateAsync(tt, q);

            var request = tt.DequeueRequest((m, c) => new { Message = m, Config = c});

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request.Config.Headers.Single().Key, Is.EqualTo("foo"));
            Assert.That(request.Config.Headers.Single().Value.Single(), Is.EqualTo("bar"));
        }

        [Test]
        public async Task GetRequestWithUserAgent()
        {
            var q = Http.Get(new Uri("https://www.example.com/"))
                        .UserAgent("Spider/1.0");

            using var tt = new TestTransport()
                .Enqueue(ZeroBytes);

            _ = await EvaluateAsync(tt, q);

            var request = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request.Config.UserAgent, Is.EqualTo("Spider/1.0"));
            Assert.That(request.Message.Headers.UserAgent, Is.EqualTo(new[] { ProductInfoHeaderValue.Parse("Spider/1.0") }));
        }

        [Test]
        public async Task ApplyConfig()
        {
            var credentials = new NetworkCredential("admin", "admin");
            var config = HttpConfig.Default.WithCredentials(credentials);
            var q = Http.Config(config)
                        .Apply(Http.Get(new Uri("https://www.example.com/")));

            using var tt = new TestTransport()
                .Enqueue(ZeroBytes);

            _ = await EvaluateAsync(tt, q);

            var request = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request.Config, Is.SameAs(config));
        }

        /*
        [Test]
        public async Task GetRequestWithCookies()
        {
            var cookie = new Cookie("name", "value");
            var tt = new TestTransport(HttpConfig.Default.WithCookies(new[] { cookie }))
                .Enqueue(ZeroBytes);

            await tt.Http.Get(new Uri("https://www.example.com/"));
            var request = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request.Config.Cookies.Single(), Is.SameAs(cookie));
        }

        [Test]
        public async Task SeparateRequestsWithCookie()
        {
            var cookie = new Cookie("name", "value");
            var tt = new TestTransport(HttpConfig.Default.WithCookies(new[] { cookie }))
                .Enqueue(ZeroBytes)
                .Enqueue(ZeroBytes);

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
        */

        [Test]
        public async Task SeparateRequestsWithConfig()
        {
            var qq = Http.Get(new Uri("https://www.example.com"))
                         .Get(new Uri("https://www.example.com/page"));

            var credentials = new NetworkCredential("admin", "admin");
            var config = HttpConfig.Default.WithCredentials(credentials);

            var q = Http.Config(config).Apply(qq);

            using var tt = new TestTransport()
                .Enqueue(ZeroBytes)
                .Enqueue(ZeroBytes);

            _ = await EvaluateAsync(tt, q);

            var request1 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });
            var request2 = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request1.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request2.Message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request1.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com")));
            Assert.That(request2.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/page")));
            Assert.That(request1.Config, Is.SameAs(config));
            Assert.That(request2.Config, Is.SameAs(config));
        }

        [Test]
        public async Task PostRequestTest()
        {
            var q = Http.Post(new Uri("https://www.example.com/"), new NameValueCollection
            {
                ["name"] = "value"
            });

            using var tt = new TestTransport()
                .Enqueue(ZeroBytes);

            _ = await EvaluateAsync(tt, q);

            var request = tt.DequeueRequest((m, c) => new { Message = m, Config = c });

            Assert.That(request.Message.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request.Message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(request.Config.Headers, Is.Empty);
            Assert.That(request.Message.Content, Is.Not.Null);
            Assert.That(await request.Message.Content.ReadAsStringAsync(), Is.EqualTo("name=value"));
        }

        [Test, Ignore("https://github.com/weblinq/WebLinq/issues/18")]
        public async Task ChainedPostRequestsWithDifferentHeaders()
        {
            var data1 = new NameValueCollection { [""] = "" };
            var data2 = new NameValueCollection { [""] = "" };

            var q = Http.SetHeader("h1","v1")
                        .Post(new Uri("https://www.example.com/"), data1)
                        .SetHeader("h2", "v2")
                        .Post(new Uri("https://www.google.com/"), data2);

            using var tt = new TestTransport()
                     .Enqueue(ZeroBytes)
                     .Enqueue(ZeroBytes);

            _ = await EvaluateAsync(tt, q);

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
            var q = Http.Post(new Uri("https://www.example.com/"), new NameValueCollection
                        {
                            ["name1"] = "value1"
                        })
                        .Post(new Uri("https://www.google.com/"), new NameValueCollection
                        {
                            ["name2"] = "value2"
                        });

            using var tt = new TestTransport()
                .Enqueue(ZeroBytes)
                .Enqueue(ZeroBytes);

            _ = await EvaluateAsync(tt, q);

            var message1 = tt.DequeueRequestMessage();
            var message2 = tt.DequeueRequestMessage();

            Assert.That(message1.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(message1.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(message1.Content, Is.Not.Null);
            Assert.That(await message1.Content.ReadAsStringAsync(), Is.EqualTo("name1=value1"));
            Assert.That(message2.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(message2.RequestUri, Is.EqualTo(new Uri("https://www.google.com/")));
            Assert.That(message2.Content, Is.Not.Null);
            Assert.That(await message2.Content.ReadAsStringAsync(), Is.EqualTo("name2=value2"));
        }

        [Test]
        public async Task PostRequestWith2NameValuePairs()
        {
            var q = Http.Post(new Uri("https://www.example.com/"), new NameValueCollection
            {
                ["name1"] = "value1",
                ["name2"] = "value2",
            });

            using var tt = new TestTransport().Enqueue(ZeroBytes);

            _ = await EvaluateAsync(tt, q);

            var message = tt.DequeueRequestMessage();

            Assert.That(message.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(message.Content, Is.Not.Null);
            Assert.That(await message.Content.ReadAsStringAsync(), Is.EqualTo("name1=value1&name2=value2"));
        }

        [Test]
        public async Task PostRequestWithEmptyNameValuePair()
        {
            var q = Http.Post(new Uri("https://www.example.com/"), new NameValueCollection
            {
                [""] = "",
            });

            using var tt = new TestTransport().Enqueue(ZeroBytes);

            _ = await EvaluateAsync(tt, q);

            var message = tt.DequeueRequestMessage();

            Assert.That(message.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(message.Content, Is.Not.Null);
            Assert.That(await message.Content.ReadAsStringAsync(), Is.EqualTo("="));
        }

        [Test]
        public async Task PostRequestWithAmpersandInNameValuePair()
        {
            var q = Http.Post(new Uri("https://www.example.com/"), new NameValueCollection
            {
                ["foo&bar"] = "baz"
            });

            using var tt = new TestTransport().Enqueue(ZeroBytes);

            _ = await EvaluateAsync(tt, q);

            var message = tt.DequeueRequestMessage();

            Assert.That(message.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(message.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(message.Content, Is.Not.Null);
            Assert.That(await message.Content.ReadAsStringAsync(), Is.EqualTo("foo%26bar=baz"));
        }

        [Test]
        public void PostNullDataTest()
        {
            Assert.Throws<ArgumentNullException>(() => Http.Post(new Uri("https://www.example.com/"), data: (string)null!));
        }

        [Test]
        public async Task SubmitDefaultMethodIsGetTest()
        {
            var data = new NameValueCollection
            {
                ["firstname"] = "Mickey",
                ["lastname"] = "Mouse"
            };
            var q = Http.Get(new Uri("https://www.example.com/"))
                        .Submit(0, data);

            using var tt = new TestTransport()
                .EnqueueHtml("""
                    <!DOCTYPE html>
                    <html>
                    <body>
                      <h2> HTML Forms </h2>
                      <form action="action_page.php">
                        <br><br>
                    </form>
                    </body>
                    </html>
                    """)
                .Enqueue(ZeroBytes);

            _ = await EvaluateAsync(tt, q);

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
            var q = Http.Get(new Uri("https://www.example.com/"))
                        .Submit(0, SubmissionData.None);

            using var tt = new TestTransport()
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
                .Enqueue(ZeroBytes);

            _ = await EvaluateAsync(tt, q);

            var message1 = tt.DequeueRequestMessage();
            var message2 = tt.DequeueRequestMessage();

            Assert.That(message1.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(message1.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(message2.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(message2.RequestUri, Is.EqualTo(new Uri("https://www.example.com/action_page.php?firstname=Mickey&lastname=Mouse")));
        }

        [Test]
        public async Task SubmitInputPostTest()
        {
            var q = Http.Get(new Uri("https://www.example.com/"))
                        .Submit(0, SubmissionData.None);

            using var tt = new TestTransport()
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
                     .Enqueue(ZeroBytes);

            _ = await EvaluateAsync(tt, q);

            var message1 = tt.DequeueRequestMessage();
            var message2 = tt.DequeueRequestMessage();

            Assert.That(message1.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(message1.RequestUri, Is.EqualTo(new Uri("https://www.example.com/")));
            Assert.That(message2.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(message2.RequestUri, Is.EqualTo(new Uri("https://www.example.com/action_page.php")));
            Assert.That(message2.Content, Is.Not.Null);
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

            var q = Http.SubmitTo(new Uri("https://www.example.org/"),
                                  Html.HtmlParser.Default.Parse(html, new Uri("https://www.example.com/")),
                                  0, null);

            using var tt = new TestTransport().Enqueue(ZeroBytes);

            _ = await EvaluateAsync(tt, q);

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

            var data = new NameValueCollection
            {
                ["firstname"] = "Mickey",
                ["lastname"] = "Mouse"
            };

            var q = Http.SubmitTo(new Uri("https://www.example.org/"),
                                  Html.HtmlParser.Default.Parse(html, new Uri("https://www.example.com/")),
                                  0, data);

            using var tt = new TestTransport().Enqueue(ZeroBytes);

            _ = await EvaluateAsync(tt, q);

            var message = tt.DequeueRequestMessage();

            Assert.That(message.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(message.RequestUri, Is.EqualTo(new Uri("https://www.example.org/?firstname=Mickey&lastname=Mouse")));
            Assert.That(message.Headers, Is.Empty);
        }

        [Test(Description = "https://github.com/weblinq/WebLinq/issues/19")]
        [TestCase(30_000)]
        [TestCase(33_000)]
        [TestCase(64_000)]
        [TestCase(65_000)]
        [TestCase(70_000)]
        public async Task PostLargeData(int size)
        {
            var q = Http.Post(new Uri("https://www.example.com/"), new NameValueCollection
            {
                ["foo"] = new('*', size)
            });

            using var tt = new TestTransport().Enqueue(ZeroBytes);

            _ = await EvaluateAsync(tt, q);

            Assert.Pass();
        }

        [Test]
        public async Task FilterTrue()
        {
            HttpFetchInfo? first = null;
            HttpFetchInfo? second = null;

            var q = Http.Get(new Uri("https://www.example.com/"))
                        .ReturnErroneousFetch()
                        .Filter(f =>
                        {
                            Assert.That(f, Is.InstanceOf<HttpFetchInfo>());
                            Assert.That(second, Is.Null);
                            first = f;
                            return f.StatusCode == HttpStatusCode.BadRequest;
                        })
                        .Filter(f =>
                        {
                            Assert.That(f, Is.InstanceOf<HttpFetchInfo>());
                            Assert.That(first, Is.Not.Null);
                            Assert.That(f, Is.SameAs(first));
                            second = f;
                            return f.ContentHeaders["Content-Type"]
                                    .Single().Split(';').First() == "text/plain";
                        });

            using var tt = new TestTransport().EnqueueText(string.Empty, HttpStatusCode.BadRequest);

            _ = await EvaluateAsync(tt, q);

            // Succeeds if it doesn't throw.
        }

        [Test]
        public async Task FilterFalse()
        {
            var q = Http.Get(new Uri("https://www.example.com/"))
                        .Filter(f => f.StatusCode == HttpStatusCode.BadRequest)
                        .Filter(_ => { Assert.Fail(); return true; });

            using var tt = new TestTransport().EnqueueText(string.Empty);

            var result = await EvaluateAsync(tt, q, stream => stream.SingleOrDefaultAsync());

            Assert.That(result, Is.Null);
        }
    }
}
