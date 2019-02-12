namespace WebLinq.Tests
{
    using System;
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class HttpUrlTests
    {
        [Test]
        public void Default()
        {
            var url = default(HttpUrl);

            Assert.That(url.ToString()   , Is.Empty);
            Assert.That(url.Protocol     , Is.EqualTo(HttpProtocol.Http));
            Assert.That(url.UserInfo     , Is.Null);
            Assert.That(url.User         , Is.Null);
            Assert.That(url.Password     , Is.Null);
            Assert.That(url.Host         , Is.Null);
            Assert.That(url.Port         , Is.Zero);
            Assert.That(url.Path         , Is.Null);
            Assert.That(url.PathSegments , Is.Empty);
            Assert.That(url.Query        , Is.Null);
            Assert.That(url.Fragment     , Is.Null);
            Assert.That(url.GetHashCode(), Is.Zero);
            Assert.That((Uri) url        , Is.Null);

            Assert.True(url.Equals(url));
            Assert.True(url.Equals(default(HttpUrl)));
            Assert.True(url.Equals((object) null));
            Assert.True(url.Equals((Uri) null));

            Assert.True(url == default(HttpUrl));
            Assert.True(url == (Uri) null);
        }

        [TestCase("http://www.example.com/",
                  /* Protocol  */ HttpProtocol.Http,
                  /* User      */ "",
                  /* Password  */ "",
                  /* Host      */ "www.example.com",
                  /* Port      */ 80,
                  /* Path      */ "/",
                  /* Query     */ "",
                  /* Fragment  */ "")]

        [TestCase("http://www.example.com/foo/bar/baz",
                  /* Protocol  */ HttpProtocol.Http,
                  /* User      */ "",
                  /* Password  */ "",
                  /* Host      */ "www.example.com",
                  /* Port      */ 80,
                  /* Path      */ "/foo/bar/baz",
                  /* Query     */ "",
                  /* Fragment  */ "")]

        [TestCase("http://www.example.com/foo?bar",
                  /* Protocol  */ HttpProtocol.Http,
                  /* User      */ "",
                  /* Password  */ "",
                  /* Host      */ "www.example.com",
                  /* Port      */ 80,
                  /* Path      */ "/foo",
                  /* Query     */ "?bar",
                  /* Fragment  */ "")]

        [TestCase("http://johndoe@www.example.com/",
                  /* Protocol  */ HttpProtocol.Http,
                  /* User      */ "johndoe",
                  /* Password  */ "",
                  /* Host      */ "www.example.com",
                  /* Port      */ 80,
                  /* Path      */ "/",
                  /* Query     */ "",
                  /* Fragment  */ "")]

        [TestCase("http://:secret@www.example.com/",
                  /* Protocol  */ HttpProtocol.Http,
                  /* User      */ "",
                  /* Password  */ "secret",
                  /* Host      */ "www.example.com",
                  /* Port      */ 80,
                  /* Path      */ "/",
                  /* Query     */ "",
                  /* Fragment  */ "")]

        [TestCase("http://johndoe:secret@www.example.com:443/foo/bar?one=1&two=2&three=3#baz",
                  /* Protocol  */ HttpProtocol.Http,
                  /* User      */ "johndoe",
                  /* Password  */ "secret",
                  /* Host      */ "www.example.com",
                  /* Port      */ 443,
                  /* Path      */ "/foo/bar",
                  /* Query     */ "?one=1&two=2&three=3",
                  /* Fragment  */ "#baz")]

        [TestCase("https://johndoe:secret@www.example.com/foo/bar?one=1&two=2&three=3#baz",
                  /* Protocol  */ HttpProtocol.Https,
                  /* User      */ "johndoe",
                  /* Password  */ "secret",
                  /* Host      */ "www.example.com",
                  /* Port      */ 443,
                  /* Path      */ "/foo/bar",
                  /* Query     */ "?one=1&two=2&three=3",
                  /* Fragment  */ "#baz")]

        public void Samples(string urlString,
                            HttpProtocol protocol,
                            string user, string password,
                            string host, int port,
                            string path, string query, string fragment)
        {
            var userInfo = user + (password.Length > 0 ? ":" + password : string.Empty);

            Test(new HttpUrl(urlString).WithUserInfo(userInfo));
            Test(HttpUrl.From(protocol, host, port, path, query, fragment).WithUserInfo(userInfo));

            void Test(HttpUrl url)
            {
                Assert.That(url.ToString()   , Is.EqualTo(urlString));
                Assert.That(url.Protocol     , Is.EqualTo(protocol));
                Assert.That(url.UserInfo     , Is.EqualTo(userInfo));
                Assert.That(url.User         , Is.EqualTo(user));
                Assert.That(url.Password     , Is.EqualTo(password));
                Assert.That(url.Host         , Is.EqualTo(host));
                Assert.That(url.Port         , Is.EqualTo(port));
                Assert.That(url.Path         , Is.EqualTo(path));
                Assert.That(url.Query        , Is.EqualTo(query));
                Assert.That(url.Fragment     , Is.EqualTo(fragment));
                Assert.That(url.GetHashCode(), Is.Not.Zero);

                var uri = (Uri) url;
                Assert.That(uri, Is.EqualTo(new Uri(urlString)));

                Assert.That(url.PathSegments, Is.EqualTo(uri.Segments));

                Assert.True(url.Equals(url));
                Assert.True(url.Equals(uri));

                Assert.False(url.Equals(new Uri("http://localhost/")));

                Assert.False(url.Equals(default(HttpUrl)));
                Assert.False(url.Equals((object) null));
                Assert.False(url.Equals((Uri) null));
            }
        }
    }
}
