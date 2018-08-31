#region Copyright (c) 2016 Atif Aziz. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

namespace WebLinq.Tests
{
    using NUnit.Framework;
    using System;
    using System.Linq;
    using System.Net;
    using MoreLinq;

    [TestFixture]
    public class HttpConfigTests
    {
        [Test]
        public void DefaultConfigHeadersIsEmpty()
        {
            Assert.That(HttpConfig.Default.Headers, Is.Empty);
        }

        [Test]
        public void DefaultConfigCookiesIsNull()
        {
            Assert.That(HttpConfig.Default.Cookies, Is.Null);
        }

        [Test]
        public void DefaultConfigIgnoreInvalidServerCertificateIsFalse()
        {
            Assert.That(HttpConfig.Default.IgnoreInvalidServerCertificate, Is.False);
        }

        [Test]
        public void WithHeader()
        {
            var config = HttpConfig.Default
                                   .WithHeader("name1", "value1")
                                   .WithHeader("name2", "value2")
                                   .WithHeader("name3", "value3");

            var entry = config.Headers.Fold((e1, e2, e3) => new
            {
                First = e3, Second = e2, Third = e1,
            });

            Assert.That(entry.First.Key, Is.EqualTo("name1"));
            Assert.That(entry.First.Value.Single(), Is.EqualTo("value1"));

            Assert.That(entry.Second.Key, Is.EqualTo("name2"));
            Assert.That(entry.Second.Value.Single(), Is.EqualTo("value2"));

            Assert.That(entry.Third.Key, Is.EqualTo("name3"));
            Assert.That(entry.Third.Value.Single(), Is.EqualTo("value3"));

            AssertConfigurationsEqual(config, HttpConfig.Default, ExceptMember.Headers);
        }

        [Test]
        public void WithHeaders()
        {
            var headers = HttpHeaderCollection.Empty
                                              .Set("name1", "value1")
                                              .Set("name2", "value2")
                                              .Set("name3", "value3");

            var config = HttpConfig.Default.WithHeaders(headers);

            Assert.That(config.Headers, Is.SameAs(headers));
            AssertConfigurationsEqual(config, HttpConfig.Default, ExceptMember.Headers);
        }

        [Test]
        public void WithTimeout()
        {
            var config = HttpConfig.Default.WithTimeout(new TimeSpan(0, 1, 0));

            Assert.That(config.Timeout, Is.EqualTo(new TimeSpan(0, 1, 0)));
            AssertConfigurationsEqual(config, HttpConfig.Default, ExceptMember.Timeout);
        }

        [Test]
        public void WithUserAgent()
        {
            var config = HttpConfig.Default.WithUserAgent("Spider/1.0");

            Assert.That(config.UserAgent, Is.EqualTo("Spider/1.0"));
            AssertConfigurationsEqual(config, HttpConfig.Default, ExceptMember.UserAgent);
        }

        [Test]
        public void WithCredentials()
        {
            var credentials = new NetworkCredential("admin", "admin");
            var config = HttpConfig.Default.WithCredentials(credentials);

            Assert.That(config.Credentials, Is.SameAs(credentials));
            AssertConfigurationsEqual(config, HttpConfig.Default, ExceptMember.Credentials);
        }

        [Test]
        public void WithUseDefaultCredentials()
        {
            var config = HttpConfig.Default.WithUseDefaultCredentials(true);

            Assert.That(config.UseDefaultCredentials, Is.True);
            Assert.That(config.Credentials, Is.EqualTo(HttpConfig.Default.Credentials));
            AssertConfigurationsEqual(config, HttpConfig.Default, ExceptMember.UseDefaultCredentials);
        }

        [Test]
        public void WithCookies()
        {
            var cookies = new[] { new Cookie("name", "value") };
            var config = HttpConfig.Default.WithCookies(cookies);

            Assert.That(config.Cookies, Is.SameAs(cookies));
            AssertConfigurationsEqual(config, HttpConfig.Default, ExceptMember.Cookies);
        }

        [Test]
        public void WithIgnoreInvalidServerCertificate()
        {
            var config = HttpConfig.Default.WithIgnoreInvalidServerCertificate(true);

            Assert.That(config.IgnoreInvalidServerCertificate, Is.True);
            AssertConfigurationsEqual(config, HttpConfig.Default, ExceptMember.IgnoreInvalidServerCertificate);
        }

        enum ExceptMember
        {
            Headers,
            Timeout,
            UseDefaultCredentials,
            Credentials,
            UserAgent,
            Cookies,
            IgnoreInvalidServerCertificate,
        }

        static void AssertConfigurationsEqual(HttpConfig actual, HttpConfig expected, ExceptMember e)
        {
            if (e != ExceptMember.Headers)
                Assert.That(actual.Headers, Is.EqualTo(expected.Headers));

            if (e != ExceptMember.Timeout)
                Assert.That(actual.Timeout, Is.EqualTo(expected.Timeout));

            if (e != ExceptMember.UseDefaultCredentials)
                Assert.That(actual.UseDefaultCredentials, Is.EqualTo(expected.UseDefaultCredentials));

            if (e != ExceptMember.Credentials)
                Assert.That(actual.Credentials, Is.SameAs(expected.Credentials));

            if (e != ExceptMember.UserAgent)
                Assert.That(actual.UserAgent, Is.EqualTo(expected.UserAgent));

            if (e != ExceptMember.Cookies)
                Assert.That(actual.Cookies, Is.SameAs(expected.Cookies));

            if (e != ExceptMember.IgnoreInvalidServerCertificate)
                Assert.That(actual.IgnoreInvalidServerCertificate, Is.EqualTo(expected.IgnoreInvalidServerCertificate));
        }
    }
}