namespace WebLinq.Tests
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    public class HttpUrlTests
    {
        public class Default
        {
            [Test] public void String          () => Assert.That(default(HttpUrl).ToString()    , Is.Empty);
            [Test] public void Protocol        () => Assert.That(default(HttpUrl).Protocol      , Is.EqualTo(HttpProtocol.Http));
            [Test] public void UriScheme       () => Assert.That(default(HttpUrl).UriScheme     , Is.Empty);
            [Test] public void UserInfo        () => Assert.That(default(HttpUrl).UserInfo      , Is.Empty);
            [Test] public void User            () => Assert.That(default(HttpUrl).User          , Is.Empty);
            [Test] public void Password        () => Assert.That(default(HttpUrl).Password      , Is.Empty);
            [Test] public void Host            () => Assert.That(default(HttpUrl).Host          , Is.Empty);
            [Test] public void Port            () => Assert.That(default(HttpUrl).Port          , Is.Zero );
            [Test] public void Path            () => Assert.That(default(HttpUrl).Path          , Is.Empty);
            [Test] public void PathSegments    () => Assert.That(default(HttpUrl).PathSegments  , Is.Empty);
            [Test] public void Query           () => Assert.That(default(HttpUrl).Query.HasValue, Is.False);
            [Test] public void Fragment        () => Assert.That(default(HttpUrl).Fragment      , Is.Empty);

            [Test] public void HashCode        () => Assert.That(default(HttpUrl).GetHashCode() , Is.Zero);

            [Test] public void UriConversion   () => Assert.That((Uri) default(HttpUrl)         , Is.Null);

            [Test] public void Equals          () => Assert.True(default(HttpUrl).Equals(default(HttpUrl)));
            [Test] public void EqualsNullObject() => Assert.True(default(HttpUrl).Equals((object) null));
            [Test] public void EqualsNullUri   () => Assert.True(default(HttpUrl).Equals((Uri) null));

            [Test] public void OpEquals        () => Assert.True(default(HttpUrl) == default(HttpUrl));
            [Test] public void OpEqualsNullUri () => Assert.True(default(HttpUrl) == (Uri) null);
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
                Assert.That(url.UriScheme    , Is.EqualTo(protocol.ToString()).IgnoreCase);
                Assert.That(url.UserInfo     , Is.EqualTo(userInfo));
                Assert.That(url.User         , Is.EqualTo(user));
                Assert.That(url.Password     , Is.EqualTo(password));
                Assert.That(url.Host         , Is.EqualTo(host));
                Assert.That(url.Port         , Is.EqualTo(port));
                Assert.That(url.Path         , Is.EqualTo(path));
                Assert.That(url.Query        , Is.EqualTo(new QueryString(query)));
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

        [Test]
        public void Format()
        {
            var date = new DateTime(2007, 6, 29);
            var url = HttpUrl.Format($@"
                http://www.example.com/
                    {date:yyyy}/
                    {date:MM}/
                    {date:dd}/
                    {{123_456_789}}/
                    {123_456_789}/
                    info.html
                    ?h={"foo bar"}
                    &date={date:MMM dd, yyyy}");

            Assert.That(url.ToString(),
                Is.EqualTo("http://www.example.com/"
                         + "2007/"
                         + "06/"
                         + "29/"
                         + "%7B123_456_789%7D/"
                         + "123456789/"
                         + "info.html"
                         + "?h=foo%20bar"
                         + "&date=Jun%2029%2C%202007"));
        }
    }
}
