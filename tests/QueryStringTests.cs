// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace WebLinq.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using Collections;

    [TestFixture]
    public class QueryStringTests
    {
        [Test]
        public void CtorThrows_IfQueryDoesNotHaveLeadingQuestionMark()
        {
            // Act and Assert
            var e = Assert.Throws<ArgumentException>(() => new QueryString("hello"));
            Assert.That(e.ParamName, Is.EqualTo("value"));
            var firstMessageLine = e.Message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).First();
            Assert.That(firstMessageLine, Is.EqualTo("The leading '?' must be included for a non-empty query."));
        }

        [Test]
        public void CtorNullOrEmpty_Success()
        {
            var query = new QueryString();
            Assert.False(query.HasValue);
            Assert.Null(query.Value);

            query = new QueryString(null);
            Assert.False(query.HasValue);
            Assert.Null(query.Value);

            query = new QueryString(string.Empty);
            Assert.False(query.HasValue);
            Assert.AreEqual(string.Empty, query.Value);
        }

        [Test]
        public void CtorJustAQuestionMark_Success()
        {
            var query = new QueryString("?");
            Assert.True(query.HasValue);
            Assert.AreEqual("?", query.Value);
        }

        [Test]
        public void ToString_EncodesHash()
        {
            var query = new QueryString("?Hello=Wor#ld");
            Assert.AreEqual("?Hello=Wor%23ld", query.ToString());
        }

        [TestCase("name", "value", "?name=value")]
        [TestCase("na me", "val ue", "?na%20me=val%20ue")]
        [TestCase("name", "", "?name=")]
        [TestCase("name", null, "?name=")]
        [TestCase("", "value", "?=value")]
        [TestCase("", "", "?=")]
        [TestCase("", null, "?=")]
        public void CreateNameValue_Success(string name, string value, string expected)
        {
            var query = QueryString.Create(name, value);
            Assert.AreEqual(expected, query.Value);
        }

        [Test]
        public void CreateFromList_Success()
        {
            var query = QueryString.Create(new[]
            {
                new KeyValuePair<string, string>("key1", "value1"),
                new KeyValuePair<string, string>("key2", "value2"),
                new KeyValuePair<string, string>("key3", "value3"),
                new KeyValuePair<string, string>("key4", null),
                new KeyValuePair<string, string>("key5", "")
            });
            Assert.AreEqual("?key1=value1&key2=value2&key3=value3&key4=&key5=", query.Value);
        }

        [Test]
        public void CreateFromListStrings_Success()
        {
            var query = QueryString.Create(new[]
            {
                new KeyValuePair<string, Strings>("key1", new Strings("value1")),
                new KeyValuePair<string, Strings>("key2", new Strings("value2")),
                new KeyValuePair<string, Strings>("key3", new Strings("value3")),
                new KeyValuePair<string, Strings>("key4", new Strings()),
                new KeyValuePair<string, Strings>("key5", new Strings("")),
            });
            Assert.AreEqual("?key1=value1&key2=value2&key3=value3&key4=&key5=", query.Value);
        }

        [Theory]
        [TestCase(null, null, null)]
        [TestCase("", "", "")]
        [TestCase(null, "?name2=value2", "?name2=value2")]
        [TestCase("", "?name2=value2", "?name2=value2")]
        [TestCase("?", "?name2=value2", "?name2=value2")]
        [TestCase("?name1=value1", null, "?name1=value1")]
        [TestCase("?name1=value1", "", "?name1=value1")]
        [TestCase("?name1=value1", "?", "?name1=value1")]
        [TestCase("?name1=value1", "?name2=value2", "?name1=value1&name2=value2")]
        public void AddQueryString_Success(string query1, string query2, string expected)
        {
            var q1 = new QueryString(query1);
            var q2 = new QueryString(query2);
            Assert.AreEqual(expected, q1.Add(q2).Value);
            Assert.AreEqual(expected, (q1 + q2).Value);
        }

        [Theory]
        [TestCase("", "", "", "?=")]
        [TestCase("", "", null, "?=")]
        [TestCase("?", "", "", "?=")]
        [TestCase("?", "", null, "?=")]
        [TestCase("?", "name2", "value2", "?name2=value2")]
        [TestCase("?", "name2", "", "?name2=")]
        [TestCase("?", "name2", null, "?name2=")]
        [TestCase("?name1=value1", "name2", "value2", "?name1=value1&name2=value2")]
        [TestCase("?name1=value1", "na me2", "val ue2", "?name1=value1&na%20me2=val%20ue2")]
        [TestCase("?name1=value1", "", "", "?name1=value1&=")]
        [TestCase("?name1=value1", "", null, "?name1=value1&=")]
        [TestCase("?name1=value1", "name2", "", "?name1=value1&name2=")]
        [TestCase("?name1=value1", "name2", null, "?name1=value1&name2=")]
        public void AddNameValue_Success(string query1, string name2, string value2, string expected)
        {
            var q1 = new QueryString(query1);
            var q2 = q1.Add(name2, value2);
            Assert.AreEqual(expected, q2.Value);
        }

        [Test]
        public void Equals_EmptyQueryStringAndDefaultQueryString()
        {
            // Act and Assert
            Assert.AreEqual(default(QueryString), QueryString.Empty);
            Assert.AreEqual(default(QueryString), QueryString.Empty);
            // explicitly checking == operator
            Assert.True(QueryString.Empty == default(QueryString));
            Assert.True(default(QueryString) == QueryString.Empty);
        }

        [Test]
        public void NotEquals_DefaultQueryStringAndNonNullQueryString()
        {
            // Arrange
            var queryString = new QueryString("?foo=1");

            // Act and Assert
            Assert.AreNotEqual(default(QueryString), queryString);
        }

        [Test]
        public void NotEquals_EmptyQueryStringAndNonNullQueryString()
        {
            // Arrange
            var queryString = new QueryString("?foo=1");

            // Act and Assert
            Assert.AreNotEqual(queryString, QueryString.Empty);
        }
    }
}
