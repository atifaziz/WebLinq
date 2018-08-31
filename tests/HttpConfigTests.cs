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
    using System.Net;

    [TestFixture]
    public class HttpConfigTests
    {
        enum Configuration
        {
            Headers,
            Timeout,
            UseDefaultCredentials,
            Credentials,
            UserAgent,
            Cookies,
            IgnoreInvalidServerCertificate,
        }

        void AssertConfigurationsExcept(HttpConfig config1, HttpConfig config2, Configuration e)
        {
            if (e != Configuration.Headers)
            {
                Assert.That(config1.Headers, Is.EqualTo(config2.Headers));
            }

            if (e != Configuration.Timeout)
            {
                Assert.That(config1.Timeout, Is.EqualTo((config2.Timeout)));
            }

            if (e != Configuration.UseDefaultCredentials)
            {
                Assert.That(config1.UseDefaultCredentials, Is.EqualTo(config2.UseDefaultCredentials));
            }

            if (e != Configuration.Credentials)
            {
                Assert.That(config1.Credentials, Is.SameAs(config2.Credentials));
            }

            if (e != Configuration.UserAgent)
                Assert.That(config1.UserAgent, Is.EqualTo(config2.UserAgent));

            if (e != Configuration.Cookies)
            {
                Assert.That(config1.Cookies, Is.EqualTo(config2.Cookies));
            }

            if (e != Configuration.IgnoreInvalidServerCertificate)
            {
                Assert.That(config1.IgnoreInvalidServerCertificate, Is.EqualTo(config2.IgnoreInvalidServerCertificate));
            }
        }

        [Test]
        public void DefaultConfigHasNoHeaders()
        {
            Assert.That(HttpConfig.Default.Headers, Is.Empty);
        }

        [Test]
        public void DefaultConfigHasNoCookies()
        {
            Assert.That(HttpConfig.Default.Cookies, Is.Empty);
        }

        [Test]
        public void DefaultConfigIgnoreInvalidServerCertificateIsFalse()
        {
            Assert.That(HttpConfig.Default.IgnoreInvalidServerCertificate, Is.False);
        }

        [Test]
        public void WithHeaderTest()
        {
            var config = HttpConfig.Default.WithHeader("name", "value");
            Assert.That(config.Headers, Is.EqualTo(new HttpHeaderCollection().Set("name", "value")));

            AssertConfigurationsExcept(config, HttpConfig.Default, Configuration.Headers);
        }

        [Test]
        public void WithHeadersTest()
        {
            var config = HttpConfig.Default.WithHeaders(new HttpHeaderCollection().Set("name1", "value1")
                                                                                         .Set("name2", "value2"));
            Assert.That(config.Headers, Is.EqualTo(new HttpHeaderCollection().Set("name1", "value1")
                                                                             .Set("name2", "value2")));

            AssertConfigurationsExcept(config, HttpConfig.Default, Configuration.Headers);
        }

        [Test]
        public void WithHeaderEqualsWithHeaders()
        {
            var config1 = HttpConfig.Default.WithHeader("name", "value");
            var config2 = HttpConfig.Default.WithHeaders(new HttpHeaderCollection().Set("name", "value"));
            Assert.That(config1.Headers, Is.EqualTo(config2.Headers));

            AssertConfigurationsExcept(config1, config2, Configuration.Headers);
            AssertConfigurationsExcept(config1, HttpConfig.Default, Configuration.Headers);
        }

        [Test]
        public void WithTimeoutTest()
        {
            var config = HttpConfig.Default.WithTimeout(new TimeSpan(0, 1, 0));
            Assert.That(config.Timeout, Is.EqualTo(new TimeSpan(0, 1, 0)));

            AssertConfigurationsExcept(config, HttpConfig.Default, Configuration.Timeout);
        }

        [Test]
        public void WithUserAgentTest()
        {
            var config = HttpConfig.Default.WithUserAgent("Spider/1.0");
            Assert.That(config.UserAgent, Is.EqualTo("Spider/1.0"));

            AssertConfigurationsExcept(config, HttpConfig.Default, Configuration.UserAgent);
        }

        [Test]
        public void WithCredentialsTest()
        {
            var credentials = new NetworkCredential("admin", "admin");
            var config = HttpConfig.Default.WithCredentials(credentials);
            Assert.That(config.Credentials, Is.SameAs(credentials));

            AssertConfigurationsExcept(config, HttpConfig.Default, Configuration.Credentials);
        }

        [Test]
        public void WithUseDefaultCredentialsTest()
        {
            var config = HttpConfig.Default.WithUseDefaultCredentials(true);
            Assert.That(config.UseDefaultCredentials, Is.EqualTo(true));
            Assert.That(config.Credentials, Is.EqualTo(HttpConfig.Default.Credentials));

            AssertConfigurationsExcept(config, HttpConfig.Default, Configuration.UseDefaultCredentials);
        }

        [Test]
        public void WithCookiesTest()
        {
            var config = HttpConfig.Default.WithCookies(new[] { new Cookie("name", "value") });
            Assert.That(config.Cookies, Is.EquivalentTo(new[] { new Cookie("name", "value") }));

            AssertConfigurationsExcept(config, HttpConfig.Default, Configuration.Cookies);
        }

        [Test]
        public void IgnoreInvalidServerCertificateTest()
        {
            var config = HttpConfig.Default.WithIgnoreInvalidServerCertificate(true);
            Assert.That(config.IgnoreInvalidServerCertificate, Is.True);

            AssertConfigurationsExcept(config, HttpConfig.Default, Configuration.IgnoreInvalidServerCertificate);
        }
    }
}