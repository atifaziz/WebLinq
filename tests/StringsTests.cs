// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NUnit.Framework;
using NAssert = NUnit.Framework.Assert;
using StringValues = WebLinq.Collections.Strings;

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
        public static IEnumerable<StringValues> DefaultOrNullStringValues
        {
            get
            {
                return new StringValues[]
                {
                    new StringValues(),
                    new StringValues(default(ImmutableArray<string>)),
                };
            }
        }

        public static IEnumerable<StringValues> EmptyStringValues
        {
            get
            {
                return new StringValues[]
                {
                    StringValues.Empty,
                    new StringValues(ImmutableArray<string>.Empty),
                    ImmutableArray<string>.Empty
                };
            }
        }

        public static IEnumerable<StringValues> FilledStringValues
        {
            get
            {
                return new StringValues[]
                {
                    new StringValues("abc"),
                    new StringValues(ImmutableArray.Create("abc")),
                    new StringValues(ImmutableArray.Create("abc", "bcd")),
                    new StringValues(ImmutableArray.Create("abc", "bcd", "foo")),
                    "abc",
                    ImmutableArray.Create("abc"),
                    ImmutableArray.Create("abc", "bcd"),
                    ImmutableArray.Create("abc", "bcd", "foo"),
                };
            }
        }

        public static IEnumerable FilledStringValuesWithExpectedStrings
        {
            get
            {
                var args = new[]
                {
                    (new StringValues(string.Empty), string.Empty ),
                    (new StringValues(ImmutableArray.Create(string.Empty)), string.Empty ),
                    (new StringValues("abc"), "abc"),
                };
                return from a in args select new object[] { a.Item1, a.Item2 };
            }
        }

        public static IEnumerable FilledStringValuesWithExpectedObjects
        {
            get
            {
                var args = new[]
                {
                    (default(StringValues), (object)null),
                    (StringValues.Empty, (object)null),
                    (new StringValues(ImmutableArray<string>.Empty), (object)null),
                    (new StringValues("abc"), (object)"abc"),
                    (new StringValues("abc"), (object)new[] { "abc" }),
                    (new StringValues(ImmutableArray.Create("abc")), (object)new[] { "abc" }),
                    (new StringValues(ImmutableArray.Create("abc", "bcd")), (object)new[] { "abc", "bcd" }),
                };
                return from a in args select new[] { a.Item1, a.Item2 };
            }
        }

        public static IEnumerable FilledStringValuesWithExpected
        {
            get
            {
                var args = new(StringValues, string[])[]
                {
                    (default(StringValues), new string[0]),
                    (StringValues.Empty, new string[0]),
                    (new StringValues(string.Empty), new[] { string.Empty }),
                    (new StringValues("abc"), new[] { "abc" }),
                    (new StringValues(ImmutableArray.Create("abc")), new[] { "abc" }),
                    (new StringValues(ImmutableArray.Create("abc", "bcd")), new[] { "abc", "bcd" }),
                    (new StringValues(ImmutableArray.Create("abc", "bcd", "foo")), new[] { "abc", "bcd", "foo" }),
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
        [TestCaseSource(nameof(DefaultOrNullStringValues))]
        [TestCaseSource(nameof(EmptyStringValues))]
        [TestCaseSource(nameof(FilledStringValues))]
        public void IsReadOnly_True(StringValues stringValues)
        {
            Assert.True(((IList<string>)stringValues).IsReadOnly);
            Assert.Throws<NotSupportedException>(() => ((IList<string>)stringValues)[0] = string.Empty);
            Assert.Throws<NotSupportedException>(() => ((ICollection<string>)stringValues).Add(string.Empty));
            Assert.Throws<NotSupportedException>(() => ((IList<string>)stringValues).Insert(0, string.Empty));
            Assert.Throws<NotSupportedException>(() => ((ICollection<string>)stringValues).Remove(string.Empty));
            Assert.Throws<NotSupportedException>(() => ((IList<string>)stringValues).RemoveAt(0));
            Assert.Throws<NotSupportedException>(() => ((ICollection<string>)stringValues).Clear());
        }

        [Theory]
        [TestCaseSource(nameof(DefaultOrNullStringValues))]
        public void DefaultOrNull_ExpectedValues(StringValues stringValues)
        {
            Assert.Empty((string[])stringValues);
        }

        [Theory]
        [TestCaseSource(nameof(DefaultOrNullStringValues))]
        [TestCaseSource(nameof(EmptyStringValues))]
        public void DefaultNullOrEmpty_ExpectedValues(StringValues stringValues)
        {
            Assert.Empty(stringValues);
            Assert.Null((string)stringValues);
            Assert.Equal((string)null, stringValues);
            Assert.Equal(string.Empty, stringValues.ToString());
            Assert.Equal(new string[0], stringValues.ToArray());

            Assert.True(StringValues.IsNullOrEmpty(stringValues));
            Assert.Throws<IndexOutOfRangeException>(() => _ = stringValues[0]);
            Assert.Throws<IndexOutOfRangeException>(() => _ = ((IList<string>)stringValues)[0]);
            Assert.Equal(string.Empty, stringValues.ToString());
            Assert.Equal(-1, ((IList<string>)stringValues).IndexOf(null));
            Assert.Equal(-1, ((IList<string>)stringValues).IndexOf(string.Empty));
            Assert.Equal(-1, ((IList<string>)stringValues).IndexOf("not there"));
            Assert.False(((ICollection<string>)stringValues).Contains(null));
            Assert.False(((ICollection<string>)stringValues).Contains(string.Empty));
            Assert.False(((ICollection<string>)stringValues).Contains("not there"));
            Assert.Empty(stringValues);
        }

        [Test]
        public void ImplicitStringConverter_Works()
        {
            string nullString = null;
            StringValues stringValues = nullString;
            Assert.Null((string)stringValues);
            Assert.Equal(new string[] { null }, (string[])stringValues);

            string aString = "abc";
            stringValues = aString;
            Assert.Single(stringValues);
            Assert.Equal(aString, stringValues);
            Assert.Equal(aString, stringValues[0]);
            Assert.Equal(aString, ((IList<string>)stringValues)[0]);
            Assert.Equal<string[]>(new string[] { aString }, stringValues);
        }

        [Test]
        public void ImplicitStringArrayConverter_Works()
        {
            ImmutableArray<string> nullStringArray = default;
            StringValues stringValues = nullStringArray;
            Assert.Empty(stringValues);
            Assert.Null((string)stringValues);
            Assert.Empty((string[])stringValues);

            string aString = "abc";
            var aStringArray = ImmutableArray.Create(aString);
            stringValues = aStringArray;
            Assert.Single(stringValues);
            Assert.Equal(aString, stringValues);
            Assert.Equal(aString, stringValues[0]);
            Assert.Equal(aString, ((IList<string>)stringValues)[0]);
            Assert.Equal(aStringArray, stringValues);

            aString = "abc";
            string bString = "bcd";
            aStringArray = ImmutableArray.Create(aString, bString);
            stringValues = aStringArray;
            Assert.Equal(2, stringValues.Count);
            Assert.Equal("abc,bcd", stringValues);
            Assert.Equal(aStringArray, stringValues);
        }

        [Theory]
        [TestCaseSource(nameof(DefaultOrNullStringValues))]
        [TestCaseSource(nameof(EmptyStringValues))]
        public void DefaultNullOrEmpty_Enumerator(StringValues stringValues)
        {
            var e = stringValues.GetEnumerator();
            Assert.Null(e.Current);
            Assert.False(e.MoveNext());
            Assert.Null(e.Current);
            Assert.False(e.MoveNext());
            Assert.False(e.MoveNext());
            Assert.False(e.MoveNext());

            var e1 = ((IEnumerable<string>)stringValues).GetEnumerator();
            Assert.Null(e1.Current);
            Assert.False(e1.MoveNext());
            Assert.Null(e1.Current);
            Assert.False(e1.MoveNext());
            Assert.False(e1.MoveNext());
            Assert.False(e1.MoveNext());

            var e2 = ((IEnumerable)stringValues).GetEnumerator();
            Assert.Null(e2.Current);
            Assert.False(e2.MoveNext());
            Assert.Null(e2.Current);
            Assert.False(e2.MoveNext());
            Assert.False(e2.MoveNext());
            Assert.False(e2.MoveNext());
        }

        [Theory]
        [TestCaseSource(nameof(FilledStringValuesWithExpected))]
        public void Enumerator(StringValues stringValues, string[] expected)
        {
            var e = stringValues.GetEnumerator();
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.True(e.MoveNext());
                Assert.Equal(expected[i], e.Current);
            }
            Assert.False(e.MoveNext());
            Assert.False(e.MoveNext());
            Assert.False(e.MoveNext());

            var e1 = ((IEnumerable<string>)stringValues).GetEnumerator();
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.True(e1.MoveNext());
                Assert.Equal(expected[i], e1.Current);
            }
            Assert.False(e1.MoveNext());
            Assert.False(e1.MoveNext());
            Assert.False(e1.MoveNext());

            var e2 = ((IEnumerable)stringValues).GetEnumerator();
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
        [TestCaseSource(nameof(FilledStringValuesWithExpected))]
        public void IndexOf(StringValues stringValues, string[] expected)
        {
            IList<string> list = stringValues;
            Assert.Equal(-1, list.IndexOf("not there"));
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(i, list.IndexOf(expected[i]));
            }
        }

        [Theory]
        [TestCaseSource(nameof(FilledStringValuesWithExpected))]
        public void Contains(StringValues stringValues, string[] expected)
        {
            ICollection<string> collection = stringValues;
            Assert.False(collection.Contains("not there"));
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.True(collection.Contains(expected[i]));
            }
        }

        [Theory]
        [TestCaseSource(nameof(DefaultOrNullStringValues))]
        [TestCaseSource(nameof(EmptyStringValues))]
        [TestCaseSource(nameof(FilledStringValues))]
        public void CopyTo_TooSmall(StringValues stringValues)
        {
            ICollection<string> collection = stringValues;
            string[] tooSmall = new string[0];

            if (collection.Count > 0)
            {
                Assert.Throws<ArgumentException>(() => collection.CopyTo(tooSmall, 0));
            }
        }

        [Theory]
        [TestCaseSource(nameof(FilledStringValuesWithExpected))]
        public void CopyTo_CorrectSize(StringValues stringValues, string[] expected)
        {
            ICollection<string> collection = stringValues;
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
        [TestCaseSource(nameof(DefaultOrNullStringValues))]
        [TestCaseSource(nameof(EmptyStringValues))]
        public void DefaultNullOrEmpty_Concat(StringValues stringValues)
        {
            var expected = ImmutableArray.Create("abc", "bcd", "foo");
            StringValues expectedStringValues = new StringValues(expected);
            Assert.Equal(expected, StringValues.Concat(stringValues, expectedStringValues));
            Assert.Equal(expected, StringValues.Concat(expectedStringValues, stringValues));


            Assert.Equal(new StringValues(ImmutableArray.Create((string) null).AddRange(expected)),
                         StringValues.Concat((string)null, in expectedStringValues));

            Assert.Equal(new StringValues(expected.Add(null)),
                         StringValues.Concat(in expectedStringValues, (string)null));

            string[] empty = new string[0];
            StringValues emptyStringValues = new StringValues(ImmutableArray<string>.Empty);
            Assert.Equal(empty, StringValues.Concat(stringValues, StringValues.Empty));
            Assert.Equal(empty, StringValues.Concat(StringValues.Empty, stringValues));
            Assert.Equal(empty, StringValues.Concat(stringValues, new StringValues()));
            Assert.Equal(empty, StringValues.Concat(new StringValues(), stringValues));

            string[] @null = { null };
            Assert.Equal(@null, StringValues.Concat((string)null, in emptyStringValues));
            Assert.Equal(@null, StringValues.Concat(in emptyStringValues, (string)null));
        }

        [Theory]
        [TestCaseSource(nameof(FilledStringValuesWithExpected))]
        public void Concat(StringValues stringValues, string[] array)
        {
            var filled = ImmutableArray.Create("abc", "bcd", "foo");

            string[] expectedPrepended = array.Concat(filled).ToArray();
            Assert.Equal(expectedPrepended, StringValues.Concat(stringValues, new StringValues(filled)));

            string[] expectedAppended = filled.Concat(array).ToArray();
            Assert.Equal(expectedAppended, StringValues.Concat(new StringValues(filled), stringValues));

            StringValues values = stringValues;
            foreach (string s in filled)
            {
                values = StringValues.Concat(in values, s);
            }
            Assert.Equal(expectedPrepended, values);

            values = stringValues;
            foreach (string s in filled.Reverse())
            {
                values = StringValues.Concat(s, in values);
            }
            Assert.Equal(expectedAppended, values);
        }

        [Test]
        public void Equals_OperatorEqual()
        {
            var equalString = "abc";

            var equalStringArray = new string[] { equalString };
            var equalStringValues = new StringValues(equalString);
            var otherStringValues = new StringValues(equalString);
            var stringArray = ImmutableArray.Create(equalString, equalString);
            var stringValuesArray = new StringValues(stringArray);

            Assert.True(equalStringValues == otherStringValues);

            Assert.True(equalStringValues == equalString);
            Assert.True(equalString == equalStringValues);

            Assert.True(equalStringValues == equalStringArray);
            Assert.True(equalStringArray == equalStringValues);

            Assert.True(stringArray == stringValuesArray);
            Assert.True(stringValuesArray == stringArray);

            Assert.False(stringValuesArray == equalString);
            Assert.False(stringValuesArray == equalStringArray);
            Assert.False(stringValuesArray == equalStringValues);
        }

        [Test]
        public void Equals_OperatorNotEqual()
        {
            var equalString = "abc";

            var equalStringArray = new string[] { equalString };
            var equalStringValues = new StringValues(equalString);
            var otherStringValues = new StringValues(equalString);
            var stringArray = ImmutableArray.Create(equalString, equalString);
            var stringValuesArray = new StringValues(stringArray);

            Assert.False(equalStringValues != otherStringValues);

            Assert.False(equalStringValues != equalString);
            Assert.False(equalString != equalStringValues);

            Assert.False(equalStringValues != equalStringArray);
            Assert.False(equalStringArray != equalStringValues);

            Assert.False(stringArray != stringValuesArray);
            Assert.False(stringValuesArray != stringArray);

            Assert.True(stringValuesArray != equalString);
            Assert.True(stringValuesArray != equalStringArray);
            Assert.True(stringValuesArray != equalStringValues);
        }

        [Test]
        public void Equals_Instance()
        {
            var equalString = "abc";

            var equalStringArray = new string[] { equalString };
            var equalStringValues = new StringValues(equalString);
            var stringArray = ImmutableArray.Create(equalString, equalString);
            var stringValuesArray = new StringValues(stringArray);

            Assert.True(equalStringValues.Equals(equalStringValues));
            Assert.True(equalStringValues.Equals(equalString));
            Assert.True(equalStringValues.Equals(equalStringArray));
            Assert.True(stringValuesArray.Equals(stringArray));
        }

        [Theory]
        [TestCaseSource(nameof(FilledStringValuesWithExpectedObjects))]
        public void Equals_ObjectEquals(StringValues stringValues, object obj)
        {
            Assert.True(stringValues == obj);
            Assert.True(obj == stringValues);
        }

        [Theory]
        [TestCaseSource(nameof(FilledStringValuesWithExpectedObjects))]
        public void Equals_ObjectNotEquals(StringValues stringValues, object obj)
        {
            Assert.False(stringValues != obj);
            Assert.False(obj != stringValues);
        }

        [Theory]
        [TestCaseSource(nameof(FilledStringValuesWithExpectedStrings))]
        public void Equals_String(StringValues stringValues, string expected)
        {
            var notEqual = new StringValues("bcd");

            Assert.True(StringValues.Equals(stringValues, expected));
            Assert.False(StringValues.Equals(stringValues, notEqual));
        }

        [Theory]
        [TestCaseSource(nameof(FilledStringValuesWithExpected))]
        public void Equals_StringArray(StringValues stringValues, string[] expected)
        {
            var notEqual = ImmutableArray.Create("bcd", "abc");

            Assert.True(StringValues.Equals(stringValues, expected));
            Assert.False(StringValues.Equals(stringValues, notEqual));
        }

        [Test]
        public void NullString()
        {
            var strings = new StringValues((string)null);

            NAssert.AreEqual(1, strings.Count);
            NAssert.Null(strings[0]);
            NAssert.Null((string)strings);
            NAssert.IsEmpty(strings.ToString());
            NAssert.AreEqual(new string[] { null }, (string[])strings);
            NAssert.AreEqual(new string[] { null }, strings.ToArray());

            var list = (IList<string>)strings;
            NAssert.AreEqual(0, list.IndexOf(null));
            NAssert.AreEqual(-1, list.IndexOf("foo"));
            NAssert.True(list.Contains(null));
            NAssert.False(list.Contains("foo"));
            var array = new[] { "foo" };
            list.CopyTo(array, 0);
            NAssert.AreEqual(null, array[0]);

            NAssert.True(StringValues.IsNullOrEmpty(strings));

            using (var e = strings.GetEnumerator())
            {
                NAssert.True(e.MoveNext());
                NAssert.Null(e.Current);
                NAssert.False(e.MoveNext());
            }
        }

        static class Assert
        {
            public static void Null(object value) => NAssert.IsNull(value);
            public static void True(bool condition) => NAssert.True(condition);
            public static void False(bool condition) => NAssert.False(condition);
            public static void Empty(IEnumerable collection) => NAssert.IsEmpty(collection);
            public static void Equal<T>(T expected, T actual) => NAssert.That(actual, Is.EqualTo(expected));
            public static void Equal(string[] expected, StringValues actual) => NAssert.That(actual, Is.EqualTo(expected));
            public static void Equal(string expected, string actual) => NAssert.AreEqual(actual, expected);
            public static void Throws<T>(TestDelegate code) where T : Exception => NAssert.Throws<T>(code);
            public static void Single<T>(IEnumerable<T> collection) => NAssert.That(collection.Count(), Is.EqualTo(1));
        }
    }
}
