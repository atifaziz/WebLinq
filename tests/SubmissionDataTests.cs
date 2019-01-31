namespace WebLinq.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using Collections;
    using NUnit.Framework;
    using static Modules.HtmlModule;

    [TestFixture]
    public class SubmissionDataTests
    {
        interface ISample<T>
        {
            (T Result, NameValueCollection Data) Exercise();
            (TException Error, NameValueCollection Data) AssertThrows<TException>()
                where TException : Exception;
        }

        sealed class Sample<T> : ISample<T>
        {
            readonly ISubmissionData<T> _submission;
            readonly NameValueCollection _data;

            public Sample(ISubmissionData<T> submission, NameValueCollection data) =>
                (_submission, _data) = (submission, data);

            public (T Result, NameValueCollection Data) Exercise() =>
                (_submission.Run(_data), _data);

            public (TException Error, NameValueCollection Data) AssertThrows<TException>()
                where TException : Exception =>
                (Assert.Throws<TException>(() => Exercise()), _data);
        }

        static class Sample
        {
            public static (T Result, NameValueCollection Data)
                Exercise<T>(ISubmissionData<T> submission) =>
                    Run(submission, t => t.Exercise());

            public static ISample<T> Create<T>(ISubmissionData<T> submission) =>
                Run(submission, t => t);

            static TResult Run<T, TResult>(ISubmissionData<T> submission,
                Func<Sample<T>, TResult> selector)
            {
                var html = ParseHtml(@"
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
                    Email:<br>
                    <input type='text' name='email' value='mickey@mouse.com' >
                    <br><br>
                    <input type='radio' name='gender' value='Male'>
                    <input type='radio' name='gender' value='Female' checked>
                    <input type='checkbox' name='vehicle' value='Bike' checked> I have a bike<br>
                    <input type='checkbox' name='vehicle' value='Car' checked> I have a car<br>
                    <input type='submit' value='Submit' >
                  </form>
                </body>
                </html>");

                var data = html.Forms.Single().GetSubmissionData();
                return selector(new Sample<T>(submission, data));
            }
        }

        [Test]
        public void Return()
        {
            var value = SubmissionData.Return("foo")
                                      .Run(new NameValueCollection());

            Assert.That(value, Is.EqualTo("foo"));
        }

        [Test]
        public void Select()
        {
            var submission =
                from n in SubmissionData.Return(42)
                select new string((char) n, n);

            var names = submission.Run(new NameValueCollection());

            Assert.That(names, Is.EqualTo(new string('*', 42)));
        }

        [Test]
        public void SelectMany()
        {
            var submission =
                from n in SubmissionData.Return(42)
                from s in SubmissionData.Return(new string((char) n, n))
                select string.Concat(n, s);

            var names = submission.Run(new NameValueCollection());

            var stars = new string('*', 42);
            Assert.That(names, Is.EqualTo("42" + stars));
        }

        [Test]
        public void For()
        {
            var submission =
                SubmissionData.For(new[] { 3, 4, 5 },
                                   e => SubmissionData.Return(e * 3));

            var values = submission.Run(new NameValueCollection());

            Assert.That(values, Is.EqualTo(new[] { 9, 12, 15 }));
        }

        [Test]
        public void Names()
        {
            var submission = SubmissionData.Names();

            var (names, data) = Sample.Exercise(submission);

            Assert.That(names, Is.EqualTo(new[] { "firstname", "lastname", "email", "gender", "vehicle" }));
            AssertData(data,
                ExpectedField("firstname", "Mickey"),
                ExpectedField("lastname" , "Mouse"),
                ExpectedField("email"    , "mickey@mouse.com"),
                ExpectedField("vehicle"  , "Bike", "Car"),
                ExpectedField("gender"   , "Female"));
        }

        [Test]
        public void GetValues()
        {
            var submission =
                from fn in SubmissionData.GetValues("firstname")
                from ln in SubmissionData.GetValues("lastname")
                from email in SubmissionData.GetValues("email")
                from gender in SubmissionData.GetValues("gender")
                from vehicle in SubmissionData.GetValues("vehicle")
                select new
                {
                    FirstName = fn,
                    LastName = ln,
                    Email = email,
                    Gender = gender,
                    Vehicle = vehicle
                };

            var (result, _) = Sample.Exercise(submission);

            Assert.That(result.FirstName.Single(), Is.EqualTo("Mickey"));
            Assert.That(result.LastName.Single() , Is.EqualTo("Mouse"));
            Assert.That(result.Email.Single()    , Is.EqualTo("mickey@mouse.com"));
            Assert.That(result.Gender.Single()   , Is.EqualTo("Female"));
            Assert.That(result.Vehicle           , Is.EqualTo(new[] { "Bike", "Car" }));
        }

        [Test]
        public void Get()
        {
            var submission =
                from fn in SubmissionData.Get("firstname")
                from ln in SubmissionData.Get("lastname")
                from email in SubmissionData.Get("email")
                select new
                {
                    FirstName = fn,
                    LastName = ln,
                    Email = email,
                };

            var (result, _) = Sample.Exercise(submission);

            Assert.That(result.FirstName, Is.EqualTo("Mickey"));
            Assert.That(result.LastName , Is.EqualTo("Mouse"));
            Assert.That(result.Email    , Is.EqualTo("mickey@mouse.com"));
        }

        [Test]
        public void Set()
        {
            var submission = SubmissionData.Set("firstname", "Minnie");

            var (_, data) = Sample.Exercise(submission);

            AssertData(data,
                ExpectedField("firstname", "Minnie"),
                ExpectedField("lastname" , "Mouse"),
                ExpectedField("email"    , "mickey@mouse.com"),
                ExpectedField("vehicle"  , "Bike", "Car"),
                ExpectedField("gender"   , "Female"));
        }

        [Test]
        public void SetValues()
        {
            var submission = SubmissionData.Set("vehicle", Strings.Values("Boat", "Van"));

            var (_, data) = Sample.Exercise(submission);

            AssertData(data,
                ExpectedField("firstname", "Mickey"),
                ExpectedField("lastname" , "Mouse"),
                ExpectedField("email"    , "mickey@mouse.com"),
                ExpectedField("vehicle"  , "Boat", "Van"),
                ExpectedField("gender"   , "Female"));
        }

        [Test]
        public void SetNames()
        {
            var submission = SubmissionData.Set(new[] { "firstname", "lastname" }, "Minnie");

            var (_, data) = Sample.Exercise(submission);

            AssertData(data,
                ExpectedField("firstname", "Minnie"),
                ExpectedField("lastname" , "Minnie"),
                ExpectedField("email"    , "mickey@mouse.com"),
                ExpectedField("vehicle"  , "Bike", "Car"),
                ExpectedField("gender"   , "Female"));
        }

        [Test]
        public void SetNonExistent()
        {
            var submission = SubmissionData.Set("foo", "bar");

            var (_, data) = Sample.Exercise(submission);

            AssertData(data,
                ExpectedField("firstname", "Mickey"),
                ExpectedField("lastname" , "Mouse"),
                ExpectedField("email"    , "mickey@mouse.com"),
                ExpectedField("vehicle"  , "Bike", "Car"),
                ExpectedField("gender"   , "Female"),
                ExpectedField("foo"      , "bar"));
        }

        [Test]
        public void Update()
        {
            var submission =
                from fn in SubmissionData.Get("firstname")
                from _ in SubmissionData.Set("firstname", fn.ToUpperInvariant()).Ignore()
                select _;

            var (_, data) = Sample.Exercise(submission);

            AssertData(data,
                ExpectedField("firstname", "MICKEY"),
                ExpectedField("lastname" , "Mouse"),
                ExpectedField("email"    , "mickey@mouse.com"),
                ExpectedField("vehicle"  , "Bike", "Car"),
                ExpectedField("gender"   , "Female"));
        }

        [Test]
        public void SetSingleWhere()
        {
            var submission =
                SubmissionData.SetSingleWhere(n => n.StartsWith("first", StringComparison.OrdinalIgnoreCase),
                                              "Minnie")
                              .Return();

            var (name, data) = Sample.Exercise(submission);

            Assert.That(name, Is.EqualTo("firstname"));
            AssertData(data,
                ExpectedField("firstname", "Minnie"),
                ExpectedField("lastname" , "Mouse"),
                ExpectedField("email"    , "mickey@mouse.com"),
                ExpectedField("vehicle"  , "Bike", "Car"),
                ExpectedField("gender"   , "Female"));
        }

        [Test]
        public void SetSingleMatching()
        {
            var submission = SubmissionData.SetSingleMatching(@"^lastname$", "bar");

            var (_, data) = Sample.Exercise(submission);

            AssertData(data,
                ExpectedField("firstname", "Mickey"),
                ExpectedField("lastname" , "bar"),
                ExpectedField("email"    , "mickey@mouse.com"),
                ExpectedField("vehicle"  , "Bike", "Car"),
                ExpectedField("gender"   , "Female"));
        }

        [Test]
        public void SetSingleMatchingMultipleMatch()
        {
            var submission = SubmissionData.SetSingleMatching(@"^[first|last]name$", "bar");

            var (_, data) = Sample.Create(submission).AssertThrows<InvalidOperationException>();

            AssertData(data,
                ExpectedField("firstname", "Mickey"),
                ExpectedField("lastname" , "Mouse"),
                ExpectedField("email"    , "mickey@mouse.com"),
                ExpectedField("vehicle"  , "Bike", "Car"),
                ExpectedField("gender"   , "Female"));
        }

        [Test]
        public void SetSingleMatchingNoneMatch()
        {
            var submission = SubmissionData.SetSingleMatching(@"^foo$", "bar");

            var (_, data) = Sample.Create(submission).AssertThrows<InvalidOperationException>();

            AssertData(data,
                ExpectedField("firstname", "Mickey"),
                ExpectedField("lastname" , "Mouse"),
                ExpectedField("email"    , "mickey@mouse.com"),
                ExpectedField("vehicle"  , "Bike", "Car"),
                ExpectedField("gender"   , "Female"));
        }

        [Test]
        public void TrySetSingleWhereMultipleMatch()
        {
            var submission =
                SubmissionData.TrySetSingleWhere(n => n.EndsWith("name", StringComparison.OrdinalIgnoreCase),
                                                 "bar");
            var (_, data) = Sample.Create(submission).AssertThrows<InvalidOperationException>();

            AssertData(data,
                ExpectedField("firstname", "Mickey"),
                ExpectedField("lastname" , "Mouse"),
                ExpectedField("email"    , "mickey@mouse.com"),
                ExpectedField("vehicle"  , "Bike", "Car"),
                ExpectedField("gender"   , "Female"));
        }

        [Test]
        public void TrySetSingleWhereNoneMatch()
        {
            var submission =
                SubmissionData.TrySetSingleWhere(n => n.StartsWith("foo", StringComparison.OrdinalIgnoreCase),
                                                 "bar")
                              .Return();

            var (name, data) = Sample.Exercise(submission);

            Assert.That(name, Is.Null);
            AssertData(data,
                ExpectedField("firstname", "Mickey"),
                ExpectedField("lastname" , "Mouse"),
                ExpectedField("email"    , "mickey@mouse.com"),
                ExpectedField("vehicle"  , "Bike", "Car"),
                ExpectedField("gender"   , "Female"));
        }

        [Test]
        public void SetFirstWhere()
        {
            var submission =
                SubmissionData.SetFirstWhere(n => n.EndsWith("name", StringComparison.OrdinalIgnoreCase), "Minnie");

            var (_, data) = Sample.Exercise(submission);

            AssertData(data,
                ExpectedField("firstname", "Minnie"),
                ExpectedField("lastname" , "Mouse"),
                ExpectedField("email"    , "mickey@mouse.com"),
                ExpectedField("vehicle"  , "Bike", "Car"),
                ExpectedField("gender"   , "Female"));
        }

        [Test]
        public void SetFirstWhereNoneMatch()
        {
            var submission =
                SubmissionData.SetFirstWhere(n => n.StartsWith("foo", StringComparison.OrdinalIgnoreCase), "bar");

            var (_, data) = Sample.Create(submission).AssertThrows<InvalidOperationException>();

            AssertData(data,
                ExpectedField("firstname", "Mickey"),
                ExpectedField("lastname" , "Mouse"),
                ExpectedField("email"    , "mickey@mouse.com"),
                ExpectedField("vehicle"  , "Bike", "Car"),
                ExpectedField("gender"   , "Female"));
        }

        [Test]
        public void SetWhere()
        {
            var submission =
                SubmissionData.SetWhere(n => n.EndsWith("name", StringComparison.OrdinalIgnoreCase), "baz");

            var (_, data) = Sample.Exercise(submission);

            AssertData(data,
               ExpectedField("firstname", "baz"),
               ExpectedField("lastname" , "baz"),
               ExpectedField("email"    , "mickey@mouse.com"),
               ExpectedField("vehicle"  , "Bike", "Car"),
               ExpectedField("gender"   , "Female"));
        }

        [Test]
        public void Merge()
        {
            var other = new NameValueCollection
            {
                ["foo"] = "bar",
                ["bar"] = "baz"
            };

            var submission = SubmissionData.Merge(other);

            var (_, data) = Sample.Exercise(submission);

            AssertData(data,
                ExpectedField("firstname", "Mickey"),
                ExpectedField("lastname" , "Mouse"),
                ExpectedField("email"    , "mickey@mouse.com"),
                ExpectedField("vehicle"  , "Bike", "Car"),
                ExpectedField("gender"   , "Female"),
                ExpectedField("foo"      , "bar"),
                ExpectedField("bar"      , "baz"));
        }

        [Test]
        public void Data()
        {
            var submission = SubmissionData.Data();
            var (result, data) = Sample.Exercise(submission);
            Assert.That(result, Is.EqualTo(data));
        }

        [Test]
        public void Clear()
        {
            var submission = SubmissionData.Clear();
            var (_, data) = Sample.Exercise(submission);
            Assert.That(data, Is.Empty);
        }

        [Test]
        public void Then()
        {
            var submission = SubmissionData.Set("firstname", "Minnie")
                                           .Then(SubmissionData.Set("email", "minnie@mouse.com"));

            var (_, data) = Sample.Exercise(submission);

            AssertData(data,
                ExpectedField("firstname", "Minnie"),
                ExpectedField("lastname" , "Mouse"),
                ExpectedField("email"    , "minnie@mouse.com"),
                ExpectedField("vehicle"  , "Bike", "Car"),
                ExpectedField("gender"   , "Female"));
        }

        [Test]
        public void Zip()
        {
            var submission =
                SubmissionData.Return(42)
                              .Zip(SubmissionData.Return('a'), ValueTuple.Create);

            var names = submission.Run(new NameValueCollection());

            Assert.That(names, Is.EqualTo((42, 'a')));
        }

        [Test]
        public void Collect()
        {
            var submission =
                SubmissionData.Collect(
                    SubmissionData.Set("firstname", "Minnie"),
                    SubmissionData.Set("email", "minnie@mouse.com"));

            var (_, data) = Sample.Exercise(submission);

            AssertData(data,
                ExpectedField("firstname", "Minnie"),
                ExpectedField("lastname" , "Mouse"),
                ExpectedField("email"    , "minnie@mouse.com"),
                ExpectedField("vehicle"  , "Bike", "Car"),
                ExpectedField("gender"   , "Female"));
        }

        [Test]
        public void CollectAsEnumerable()
        {
            var sets = new[]
            {
                SubmissionData.Set("firstname", "Minnie"),
                SubmissionData.Set("email", "minnie@mouse.com"),
            };

            var submission = SubmissionData.Collect(sets.AsEnumerable());

            var (_, data) = Sample.Exercise(submission);

            AssertData(data,
                ExpectedField("firstname", "Minnie"),
                ExpectedField("lastname" , "Mouse"),
                ExpectedField("email"    , "minnie@mouse.com"),
                ExpectedField("vehicle"  , "Bike", "Car"),
                ExpectedField("gender"   , "Female"));
        }

        static (string Name, IEnumerable<string> Values)
            ExpectedField(string name, params string[] values) =>
            (name, values);

        static void AssertData(NameValueCollection data,
            params (string Name, IEnumerable<string> Values)[] expectedFields)
        {
            Assert.That(data.Count, Is.EqualTo(expectedFields.Length),
                        "Field count mismatch.");

            foreach (var (name, expected) in expectedFields)
            {
                var actual = data.GetValues(name);

                Assert.That(actual, Is.Not.Null,
                            "Field '{0}' is absent.", name);

                Assert.That(actual, Is.EqualTo(expected),
                            "Field '{0}' value mismatch.", name);
            }
        }
    }
}
