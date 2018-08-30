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
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Net;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using static Modules.HttpModule;

    
    [TestFixture]
    public class HttpConfigTests
    {
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
            HttpConfig config = HttpConfig.Default.WithHeader("name", "value");
            Assert.That(config.Headers, Is.EquivalentTo(new HttpHeaderCollection().Set("name", "value")));

            Assert.That(config.Timeout, Is.EqualTo((HttpConfig.Default.Timeout)));
            Assert.That(config.UseDefaultCredentials, Is.EqualTo(HttpConfig.Default.UseDefaultCredentials));
            Assert.That(config.Credentials, Is.SameAs(HttpConfig.Default.Credentials));
            Assert.That(config.UserAgent, Is.EqualTo(HttpConfig.Default.UserAgent));
            Assert.That(config.Cookies, Is.EqualTo(HttpConfig.Default.Cookies));
            Assert.That(config.IgnoreInvalidServerCertificate, Is.EqualTo(HttpConfig.Default.IgnoreInvalidServerCertificate));
        }

        [Test]
        public void WithHeadersTest()
        {            
            HttpConfig config = HttpConfig.Default.WithHeaders(new HttpHeaderCollection().Set("name1", "value1")
                                                                                         .Set("name2", "value2"));
            Assert.That(config.Headers, Is.EquivalentTo(new HttpHeaderCollection().Set("name1", "value1")
                                                                                  .Set("name2", "value2")));


            Assert.That(config.Timeout, Is.EqualTo((HttpConfig.Default.Timeout)));
            Assert.That(config.UseDefaultCredentials, Is.EqualTo(HttpConfig.Default.UseDefaultCredentials));
            Assert.That(config.Credentials, Is.SameAs(HttpConfig.Default.Credentials));
            Assert.That(config.UserAgent, Is.EqualTo(HttpConfig.Default.UserAgent));
            Assert.That(config.Cookies, Is.EqualTo(HttpConfig.Default.Cookies));
            Assert.That(config.IgnoreInvalidServerCertificate, Is.EqualTo(HttpConfig.Default.IgnoreInvalidServerCertificate));
        }

        [Test]
        public void WithHeaderEqualsWithHeaders()
        {
            HttpConfig config = HttpConfig.Default.WithHeader("name", "value");
            HttpConfig config2 = HttpConfig.Default.WithHeaders(new HttpHeaderCollection().Set("name", "value"));
            Assert.That(config.Headers, Is.EquivalentTo(config2.Headers));

            Assert.That(config.Timeout, Is.EqualTo(config2.Timeout));
            Assert.That(config.UseDefaultCredentials, Is.EqualTo(config2.UseDefaultCredentials));
            Assert.That(config.Credentials, Is.SameAs(config2.Credentials));
            Assert.That(config.UserAgent, Is.EqualTo(config2.UserAgent));
            Assert.That(config.Cookies, Is.EqualTo(config2.Cookies));
            Assert.That(config.IgnoreInvalidServerCertificate, Is.EqualTo(config2.IgnoreInvalidServerCertificate));
        }

        [Test]
        public void WithTimeoutTest()
        {
            HttpConfig config = HttpConfig.Default.WithTimeout(new TimeSpan(0, 1, 0));
            Assert.That(config.Timeout, Is.EqualTo(new TimeSpan(0, 1, 0)));

            Assert.That(config.Headers, Is.EquivalentTo(HttpConfig.Default.Headers));
            Assert.That(config.UseDefaultCredentials, Is.EqualTo(HttpConfig.Default.UseDefaultCredentials));
            Assert.That(config.Credentials, Is.SameAs(HttpConfig.Default.Credentials));
            Assert.That(config.UserAgent, Is.EqualTo(HttpConfig.Default.UserAgent));
            Assert.That(config.Cookies, Is.EqualTo(HttpConfig.Default.Cookies));
            Assert.That(config.IgnoreInvalidServerCertificate, Is.EqualTo(HttpConfig.Default.IgnoreInvalidServerCertificate));
        }

        [Test]
        public void WithUserAgentTest()
        {
            HttpConfig config = HttpConfig.Default.WithUserAgent("Spider/1.0");
            Assert.That(config.UserAgent, Is.EqualTo("Spider/1.0"));

            Assert.That(config.Headers, Is.EquivalentTo(HttpConfig.Default.Headers));
            Assert.That(config.Timeout, Is.EqualTo((HttpConfig.Default.Timeout)));
            Assert.That(config.UseDefaultCredentials, Is.EqualTo(HttpConfig.Default.UseDefaultCredentials));
            Assert.That(config.Credentials, Is.SameAs(HttpConfig.Default.Credentials));
            Assert.That(config.Cookies, Is.EqualTo(HttpConfig.Default.Cookies));
            Assert.That(config.IgnoreInvalidServerCertificate, Is.EqualTo(HttpConfig.Default.IgnoreInvalidServerCertificate));
        }

        [Test]
        public void WithCredentialsTest()
        {
            var credentials = new NetworkCredential("admin", "admin");
            HttpConfig config = HttpConfig.Default.WithCredentials(credentials);
            Assert.That(config.Credentials, Is.SameAs(credentials));

            Assert.That(config.Headers, Is.EquivalentTo(HttpConfig.Default.Headers));
            Assert.That(config.Timeout, Is.EqualTo((HttpConfig.Default.Timeout)));
            Assert.That(config.UseDefaultCredentials, Is.EqualTo(HttpConfig.Default.UseDefaultCredentials));
            Assert.That(config.UserAgent, Is.EqualTo(HttpConfig.Default.UserAgent));
            Assert.That(config.Cookies, Is.EqualTo(HttpConfig.Default.Cookies));
            Assert.That(config.IgnoreInvalidServerCertificate, Is.EqualTo(HttpConfig.Default.IgnoreInvalidServerCertificate));
        }

        [Test]
        public void WithUseDefaultCredentialsTest()
        {
            HttpConfig config = HttpConfig.Default.WithUseDefaultCredentials(true);
            Assert.That(config.UseDefaultCredentials, Is.EqualTo(true));
            Assert.That(config.Credentials, Is.EqualTo(HttpConfig.Default.Credentials));

            Assert.That(config.Headers, Is.EquivalentTo(HttpConfig.Default.Headers));
            Assert.That(config.Timeout, Is.EqualTo((HttpConfig.Default.Timeout)));
            Assert.That(config.Credentials, Is.SameAs(HttpConfig.Default.Credentials));
            Assert.That(config.UserAgent, Is.EqualTo(HttpConfig.Default.UserAgent));
            Assert.That(config.Cookies, Is.EqualTo(HttpConfig.Default.Cookies));
            Assert.That(config.IgnoreInvalidServerCertificate, Is.EqualTo(HttpConfig.Default.IgnoreInvalidServerCertificate));
        }

        [Test]
        public void WithCookiesTest()
        {
            HttpConfig config = HttpConfig.Default.WithCookies(new[] { new Cookie("name", "value") });
            Assert.That(config.Cookies, Is.EquivalentTo(new[] { new Cookie("name", "value") }));

            Assert.That(config.Headers, Is.EquivalentTo(HttpConfig.Default.Headers));
            Assert.That(config.Timeout, Is.EqualTo((HttpConfig.Default.Timeout)));
            Assert.That(config.Credentials, Is.SameAs(HttpConfig.Default.Credentials));
            Assert.That(config.UseDefaultCredentials, Is.EqualTo(HttpConfig.Default.UseDefaultCredentials));
            Assert.That(config.UserAgent, Is.EqualTo(HttpConfig.Default.UserAgent));
            Assert.That(config.IgnoreInvalidServerCertificate, Is.EqualTo(HttpConfig.Default.IgnoreInvalidServerCertificate));
        }

        [Test]
        public void IgnoreInvalidServerCertificateTest()
        {
            HttpConfig config = HttpConfig.Default.WithIgnoreInvalidServerCertificate(true);
            Assert.That(config.IgnoreInvalidServerCertificate, Is.True);
  
            Assert.That(config.Headers, Is.EquivalentTo(HttpConfig.Default.Headers));
            Assert.That(config.Timeout, Is.EqualTo((HttpConfig.Default.Timeout)));
            Assert.That(config.Credentials, Is.SameAs(HttpConfig.Default.Credentials));
            Assert.That(config.UseDefaultCredentials, Is.EqualTo(HttpConfig.Default.UseDefaultCredentials));
            Assert.That(config.UserAgent, Is.EqualTo(HttpConfig.Default.UserAgent));
            Assert.That(config.Cookies, Is.EqualTo(HttpConfig.Default.Cookies));
        }
    }
}