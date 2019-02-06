// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NUnit.Framework;
using NAssert = NUnit.Framework.Assert;
using WebLinq.Collections;

namespace WebLinq.Tests
{
    // Source: https://github.com/aspnet/Extensions/blob/7ce647cfa3287e31497b72643eee28531eed1b7f/src/Primitives/test/StringValuesTests.cs
    //
    // - Moved namespace to one belonging to this project.
    // - Renamed StringValues to Strings.
    // - Modified to work with NUnit instead of XUnit.
    // - Adapted according to changes in Strings implementation.

    public class StringsTests
    {
        public static IEnumerable<Strings> DefaultOrNullStrings
        {
            get
            {
                return new Strings[]
                {
                    new Strings(),
                    new Strings(default(ImmutableArray<string>)),
                };
            }
        }

        public static IEnumerable<Strings> EmptyStrings
        {
            get
            {
                return new Strings[]
                {
                    Strings.Empty,
                    new Strings(ImmutableArray<string>.Empty),
                    ImmutableArray<string>.Empty
                };
            }
        }

        public static IEnumerable<Strings> FilledStrings
        {
            get
            {
                return new Strings[]
                {
                    new Strings("abc"),
                    new Strings(ImmutableArray.Create("abc")),
                    new Strings(ImmutableArray.Create("abc", "bcd")),
                    new Strings(ImmutableArray.Create("abc", "bcd", "foo")),
                    "abc",
                    ImmutableArray.Create("abc"),
                    ImmutableArray.Create("abc", "bcd"),
                    ImmutableArray.Create("abc", "bcd", "foo"),
                };
            }
        }

        public static IEnumerable FilledStringsWithExpectedStrings
        {
            get
            {
                var args = new[]
                {
                    (new Strings(string.Empty), string.Empty ),
                    (new Strings(ImmutableArray.Create(string.Empty)), string.Empty ),
                    (new Strings("abc"), "abc"),
                };
                return from a in args select new object[] { a.Item1, a.Item2 };
            }
        }

        public static IEnumerable FilledStringsWithExpectedObjects
        {
            get
            {
                var args = new[]
                {
                    (default(Strings), (object)null),
                    (Strings.Empty, (object)null),
                    (new Strings(ImmutableArray<string>.Empty), (object)null),
                    (new Strings("abc"), (object)"abc"),
                    (new Strings("abc"), (object)new[] { "abc" }),
                    (new Strings(ImmutableArray.Create("abc")), (object)new[] { "abc" }),
                    (new Strings(ImmutableArray.Create("abc", "bcd")), (object)new[] { "abc", "bcd" }),
                };
                return from a in args select new[] { a.Item1, a.Item2 };
            }
        }

        public static IEnumerable FilledStringsWithExpected
        {
            get
            {
                var args = new(Strings, string[])[]
                {
                    (default(Strings), new string[0]),
                    (Strings.Empty, new string[0]),
                    (new Strings(string.Empty), new[] { string.Empty }),
                    (new Strings("abc"), new[] { "abc" }),
                    (new Strings(ImmutableArray.Create("abc")), new[] { "abc" }),
                    (new Strings(ImmutableArray.Create("abc", "bcd")), new[] { "abc", "bcd" }),
                    (new Strings(ImmutableArray.Create("abc", "bcd", "foo")), new[] { "abc", "bcd", "foo" }),
                    (string.Empty, new[] { string.Empty }),
                    ("abc", new[] { "abc" }),
                    (ImmutableArray.Create("abc"), new[] { "abc" }),
                    (ImmutableArray.Create("abc", "bcd"), new[] { "abc", "bcd" }),
                    (ImmutableArray.Create("abc", "bcd", "foo"), new[] { "abc", "bcd", "foo" }),
                };
                return from a in args select new object[] { a.Item1, a.Item2 };
            }
        }

        [Theory]
        [TestCaseSource(nameof(DefaultOrNullStrings))]
        [TestCaseSource(nameof(EmptyStrings))]
        [TestCaseSource(nameof(FilledStrings))]
        public void IsReadOnly_True(Strings strings)
        {
            Assert.True(((IList<string>)strings).IsReadOnly);
            Assert.Throws<NotSupportedException>(() => ((IList<string>)strings)[0] = string.Empty);
            Assert.Throws<NotSupportedException>(() => ((ICollection<string>)strings).Add(string.Empty));
            Assert.Throws<NotSupportedException>(() => ((IList<string>)strings).Insert(0, string.Empty));
            Assert.Throws<NotSupportedException>(() => ((ICollection<string>)strings).Remove(string.Empty));
            Assert.Throws<NotSupportedException>(() => ((IList<string>)strings).RemoveAt(0));
            Assert.Throws<NotSupportedException>(() => ((ICollection<string>)strings).Clear());
        }

        [Theory]
        [TestCaseSource(nameof(DefaultOrNullStrings))]
        public void DefaultOrNull_ExpectedValues(Strings strings)
        {
            Assert.Empty((string[])strings);
        }

        [Theory]
        [TestCaseSource(nameof(DefaultOrNullStrings))]
        [TestCaseSource(nameof(EmptyStrings))]
        public void DefaultNullOrEmpty_ExpectedValues(Strings strings)
        {
            Assert.Empty(strings);
            Assert.Null((string)strings);
            Assert.Equal((string)null, strings);
            Assert.Equal(string.Empty, strings.ToString());
            Assert.Equal(new string[0], strings.ToArray());

            Assert.True(Strings.IsNullOrEmpty(strings));
            Assert.Throws<IndexOutOfRangeException>(() => _ = strings[0]);
            Assert.Throws<IndexOutOfRangeException>(() => _ = ((IList<string>)strings)[0]);
            Assert.Equal(string.Empty, strings.ToString());
            Assert.Equal(-1, ((IList<string>)strings).IndexOf(null));
            Assert.Equal(-1, ((IList<string>)strings).IndexOf(string.Empty));
            Assert.Equal(-1, ((IList<string>)strings).IndexOf("not there"));
            Assert.False(((ICollection<string>)strings).Contains(null));
            Assert.False(((ICollection<string>)strings).Contains(string.Empty));
            Assert.False(((ICollection<string>)strings).Contains("not there"));
            Assert.Empty(strings);
        }

        [Test]
        public void ImplicitStringConverter_Works()
        {
            string nullString = null;
            Strings strings = nullString;
            Assert.Null((string)strings);
            Assert.Equal(new string[] { null }, (string[])strings);

            string aString = "abc";
            strings = aString;
            Assert.Single(strings);
            Assert.Equal(aString, strings);
            Assert.Equal(aString, strings[0]);
            Assert.Equal(aString, ((IList<string>)strings)[0]);
            Assert.Equal<string[]>(new string[] { aString }, strings);
        }

        [Test]
        public void ImplicitStringArrayConverter_Works()
        {
            ImmutableArray<string> nullStringArray = default;
            Strings strings = nullStringArray;
            Assert.Empty(strings);
            Assert.Null((string)strings);
            Assert.Empty((string[])strings);

            string aString = "abc";
            var aStringArray = ImmutableArray.Create(aString);
            strings = aStringArray;
            Assert.Single(strings);
            Assert.Equal(aString, strings);
            Assert.Equal(aString, strings[0]);
            Assert.Equal(aString, ((IList<string>)strings)[0]);
            Assert.Equal(aStringArray, strings);

            aString = "abc";
            string bString = "bcd";
            aStringArray = ImmutableArray.Create(aString, bString);
            strings = aStringArray;
            Assert.Equal(2, strings.Count);
            Assert.Equal("abc,bcd", strings);
            Assert.Equal(aStringArray, strings);
        }

        [Theory]
        [TestCaseSource(nameof(DefaultOrNullStrings))]
        [TestCaseSource(nameof(EmptyStrings))]
        public void DefaultNullOrEmpty_Enumerator(Strings strings)
        {
            var e = strings.GetEnumerator();
            Assert.Null(e.Current);
            Assert.False(e.MoveNext());
            Assert.Null(e.Current);
            Assert.False(e.MoveNext());
            Assert.False(e.MoveNext());
            Assert.False(e.MoveNext());

            var e1 = ((IEnumerable<string>)strings).GetEnumerator();
            Assert.Null(e1.Current);
            Assert.False(e1.MoveNext());
            Assert.Null(e1.Current);
            Assert.False(e1.MoveNext());
            Assert.False(e1.MoveNext());
            Assert.False(e1.MoveNext());

            var e2 = ((IEnumerable)strings).GetEnumerator();
            Assert.Null(e2.Current);
            Assert.False(e2.MoveNext());
            Assert.Null(e2.Current);
            Assert.False(e2.MoveNext());
            Assert.False(e2.MoveNext());
            Assert.False(e2.MoveNext());
        }

        [Theory]
        [TestCaseSource(nameof(FilledStringsWithExpected))]
        public void Enumerator(Strings strings, string[] expected)
        {
            var e = strings.GetEnumerator();
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.True(e.MoveNext());
                Assert.Equal(expected[i], e.Current);
            }
            Assert.False(e.MoveNext());
            Assert.False(e.MoveNext());
            Assert.False(e.MoveNext());

            var e1 = ((IEnumerable<string>)strings).GetEnumerator();
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.True(e1.MoveNext());
                Assert.Equal(expected[i], e1.Current);
            }
            Assert.False(e1.MoveNext());
            Assert.False(e1.MoveNext());
            Assert.False(e1.MoveNext());

            var e2 = ((IEnumerable)strings).GetEnumerator();
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.True(e2.MoveNext());
                Assert.Equal(expected[i], e2.Current);
            }
            Assert.False(e2.MoveNext());
            Assert.False(e2.MoveNext());
            Assert.False(e2.MoveNext());
        }

        [Theory]
        [TestCaseSource(nameof(FilledStringsWithExpected))]
        public void IndexOf(Strings strings, string[] expected)
        {
            IList<string> list = strings;
            Assert.Equal(-1, list.IndexOf("not there"));
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(i, list.IndexOf(expected[i]));
            }
        }

        [Theory]
        [TestCaseSource(nameof(FilledStringsWithExpected))]
        public void Contains(Strings strings, string[] expected)
        {
            ICollection<string> collection = strings;
            Assert.False(collection.Contains("not there"));
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.True(collection.Contains(expected[i]));
            }
        }

        [Theory]
        [TestCaseSource(nameof(DefaultOrNullStrings))]
        [TestCaseSource(nameof(EmptyStrings))]
        [TestCaseSource(nameof(FilledStrings))]
        public void CopyTo_TooSmall(Strings strings)
        {
            ICollection<string> collection = strings;
            string[] tooSmall = new string[0];

            if (collection.Count > 0)
            {
                Assert.Throws<ArgumentException>(() => collection.CopyTo(tooSmall, 0));
            }
        }

        [Theory]
        [TestCaseSource(nameof(FilledStringsWithExpected))]
        public void CopyTo_CorrectSize(Strings strings, string[] expected)
        {
            ICollection<string> collection = strings;
            string[] actual = new string[expected.Length];

            if (collection.Count > 0)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => collection.CopyTo(actual, -1));
                Assert.Throws<ArgumentException>(() => collection.CopyTo(actual, actual.Length + 1));
            }
            collection.CopyTo(actual, 0);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [TestCaseSource(nameof(DefaultOrNullStrings))]
        [TestCaseSource(nameof(EmptyStrings))]
        public void DefaultNullOrEmpty_Concat(Strings strings)
        {
            var expected = ImmutableArray.Create("abc", "bcd", "foo");
            Strings expectedStrings = new Strings(expected);
            Assert.Equal(expected, Strings.Concat(strings, expectedStrings));
            Assert.Equal(expected, Strings.Concat(expectedStrings, strings));


            Assert.Equal(new Strings(ImmutableArray.Create((string) null).AddRange(expected)),
                         Strings.Concat((string)null, in expectedStrings));

            Assert.Equal(new Strings(expected.Add(null)),
                         Strings.Concat(in expectedStrings, (string)null));

            string[] empty = new string[0];
            Strings emptyStrings = new Strings(ImmutableArray<string>.Empty);
            Assert.Equal(empty, Strings.Concat(strings, Strings.Empty));
            Assert.Equal(empty, Strings.Concat(Strings.Empty, strings));
            Assert.Equal(empty, Strings.Concat(strings, new Strings()));
            Assert.Equal(empty, Strings.Concat(new Strings(), strings));

            string[] @null = { null };
            Assert.Equal(@null, Strings.Concat((string)null, in emptyStrings));
            Assert.Equal(@null, Strings.Concat(in emptyStrings, (string)null));
        }

        [Theory]
        [TestCaseSource(nameof(FilledStringsWithExpected))]
        public void Concat(Strings strings, string[] array)
        {
            var filled = ImmutableArray.Create("abc", "bcd", "foo");

            string[] expectedPrepended = array.Concat(filled).ToArray();
            Assert.Equal(expectedPrepended, Strings.Concat(strings, new Strings(filled)));

            string[] expectedAppended = filled.Concat(array).ToArray();
            Assert.Equal(expectedAppended, Strings.Concat(new Strings(filled), strings));

            Strings values = strings;
            foreach (string s in filled)
            {
                values = Strings.Concat(in values, s);
            }
            Assert.Equal(expectedPrepended, values);

            values = strings;
            foreach (string s in filled.Reverse())
            {
                values = Strings.Concat(s, in values);
            }
            Assert.Equal(expectedAppended, values);
        }

        [Test]
        public void Equals_OperatorEqual()
        {
            var equalString = "abc";

            var equalStringArray = new string[] { equalString };
            var equalStrings = new Strings(equalString);
            var otherStrings = new Strings(equalString);
            var stringArray = ImmutableArray.Create(equalString, equalString);
            var stringValuesArray = new Strings(stringArray);

            Assert.True(equalStrings == otherStrings);

            Assert.True(equalStrings == equalString);
            Assert.True(equalString == equalStrings);

            Assert.True(equalStrings == equalStringArray);
            Assert.True(equalStringArray == equalStrings);

            Assert.True(stringArray == stringValuesArray);
            Assert.True(stringValuesArray == stringArray);

            Assert.False(stringValuesArray == equalString);
            Assert.False(stringValuesArray == equalStringArray);
            Assert.False(stringValuesArray == equalStrings);
        }

        [Test]
        public void Equals_OperatorNotEqual()
        {
            var equalString = "abc";

            var equalStringArray = new string[] { equalString };
            var equalStrings = new Strings(equalString);
            var otherStrings = new Strings(equalString);
            var stringArray = ImmutableArray.Create(equalString, equalString);
            var stringValuesArray = new Strings(stringArray);

            Assert.False(equalStrings != otherStrings);

            Assert.False(equalStrings != equalString);
            Assert.False(equalString != equalStrings);

            Assert.False(equalStrings != equalStringArray);
            Assert.False(equalStringArray != equalStrings);

            Assert.False(stringArray != stringValuesArray);
            Assert.False(stringValuesArray != stringArray);

            Assert.True(stringValuesArray != equalString);
            Assert.True(stringValuesArray != equalStringArray);
            Assert.True(stringValuesArray != equalStrings);
        }

        [Test]
        public void Equals_Instance()
        {
            var equalString = "abc";

            var equalStringArray = new string[] { equalString };
            var equalStrings = new Strings(equalString);
            var stringArray = ImmutableArray.Create(equalString, equalString);
            var stringValuesArray = new Strings(stringArray);

            Assert.True(equalStrings.Equals(equalStrings));
            Assert.True(equalStrings.Equals(equalString));
            Assert.True(equalStrings.Equals(equalStringArray));
            Assert.True(stringValuesArray.Equals(stringArray));
        }

        [Theory]
        [TestCaseSource(nameof(FilledStringsWithExpectedObjects))]
        public void Equals_ObjectEquals(Strings strings, object obj)
        {
            Assert.True(strings == obj);
            Assert.True(obj == strings);
        }

        [Theory]
        [TestCaseSource(nameof(FilledStringsWithExpectedObjects))]
        public void Equals_ObjectNotEquals(Strings strings, object obj)
        {
            Assert.False(strings != obj);
            Assert.False(obj != strings);
        }

        [Theory]
        [TestCaseSource(nameof(FilledStringsWithExpectedStrings))]
        public void Equals_String(Strings strings, string expected)
        {
            var notEqual = new Strings("bcd");

            Assert.True(Strings.Equals(strings, expected));
            Assert.False(Strings.Equals(strings, notEqual));
        }

        [Theory]
        [TestCaseSource(nameof(FilledStringsWithExpected))]
        public void Equals_StringArray(Strings strings, string[] expected)
        {
            var notEqual = ImmutableArray.Create("bcd", "abc");

            Assert.True(Strings.Equals(strings, expected));
            Assert.False(Strings.Equals(strings, notEqual));
        }

        [Test]
        public void NullString()
        {
            var strings = new Strings((string)null);

            Assert.AreEqual(1, strings.Count);
            Assert.Null(strings[0]);
            Assert.Null((string)strings);
            Assert.IsEmpty(strings.ToString());
            Assert.AreEqual(new string[] { null }, (string[])strings);
            Assert.AreEqual(new string[] { null }, strings.ToArray());

            var list = (IList<string>)strings;
            Assert.AreEqual(0, list.IndexOf(null));
            Assert.AreEqual(-1, list.IndexOf("foo"));
            Assert.True(list.Contains(null));
            Assert.False(list.Contains("foo"));
            var array = new[] { "foo" };
            list.CopyTo(array, 0);
            Assert.AreEqual(null, array[0]);

            Assert.True(Strings.IsNullOrEmpty(strings));

            using (var e = strings.GetEnumerator())
            {
                Assert.True(e.MoveNext());
                Assert.Null(e.Current);
                Assert.False(e.MoveNext());
            }
        }

        class Assert : NAssert
        {
            public static void Empty(IEnumerable collection) => IsEmpty(collection);
            public static void Equal<T>(T expected, T actual) => That(actual, Is.EqualTo(expected));
            public static void Equal(string[] expected, Strings actual) => That(actual, Is.EqualTo(expected));
            public static void Equal(string expected, string actual) => AreEqual(actual, expected);
            public static void Single<T>(IEnumerable<T> collection) => That(collection.Count(), Is.EqualTo(1));
        }
    }
}
