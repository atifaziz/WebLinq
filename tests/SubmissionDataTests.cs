namespace WebLinq.Tests
{
    using System;
    using System.Collections.Specialized;
    using System.Linq;
    using NUnit.Framework;
    using static Modules.HtmlModule;

    [TestFixture]
    public class SubmissionDataTests
    {
        NameValueCollection _data;

        [SetUp]
        public void Init()
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

            _data = html.Forms.Single().GetSubmissionData();
        }

        [Test]
        public void Return()
        {
            var submission = SubmissionData.Return("foo");
            var data = _data;
            var value = submission.Run(data);

            Assert.That(value, Is.EqualTo("foo"));
        }

        [Test]
        public void Select()
        {
            var submission =
                from n in SubmissionData.Return(42)
                select new string((char) n, n);

            var data = _data;
            var names = submission.Run(data);
            Assert.That(names, Is.EqualTo(new string('*', 42)));
        }

        [Test]
        public void SelectMany()
        {
            var submission =
                from n in SubmissionData.Return(42)
                from s in SubmissionData.Return(new string((char) n, n))
                select string.Concat(n, s);

            var data = _data;
            var names = submission.Run(data);
            var stars = new string('*', 42);

            Assert.That(names, Is.EqualTo("42" + stars));
        }

        [Test]
        public void For()
        {
            var source = new[] { 3, 4, 5 };
            var submission = SubmissionData.For(source, e => SubmissionData.Return(e * 3));

            var data = _data;
            var values = submission.Run(data);

            Assert.That(values, Is.EqualTo(new[] { 9, 12, 15 }));
        }

        [Test]
        public void Names()
        {
            var submission = SubmissionData.Names();

            var data = _data;
            var names = submission.Run(data);

            Assert.That(names, Is.EqualTo(new[] { "firstname", "lastname", "email", "gender", "vehicle" }));
            Assert.That(data["firstname"], Is.EqualTo("Mickey"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
            Assert.That(data["email"], Is.EqualTo("mickey@mouse.com"));
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

            var data = _data;
            var result = submission.Run(data);

            Assert.That(result.FirstName.Single(), Is.EqualTo("Mickey"));
            Assert.That(result.LastName.Single(), Is.EqualTo("Mouse"));
            Assert.That(result.Email.Single(), Is.EqualTo("mickey@mouse.com"));
            Assert.That(result.Gender.Single(), Is.EqualTo("Female"));
            Assert.That(result.Vehicle, Is.EqualTo(new[] { "Bike", "Car" }));
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

            var data = _data;
            var result = submission.Run(data);

            Assert.That(result.FirstName, Is.EqualTo("Mickey"));
            Assert.That(result.LastName, Is.EqualTo("Mouse"));
            Assert.That(result.Email, Is.EqualTo("mickey@mouse.com"));
        }

        [Test]
        public void Set()
        {
            var submission = SubmissionData.Set("firstname", "Minnie");

            var data = _data;
            submission.Run(data);

            Assert.That(data.Count, Is.EqualTo(5));
            Assert.That(data["firstname"], Is.EqualTo("Minnie"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
            Assert.That(data["email"], Is.EqualTo("mickey@mouse.com"));
        }

        [Test]
        public void SetNames()
        {
            var submission = SubmissionData.Set(new[] { "firstname", "lastname" }, "Minnie");

            var data = _data;
            submission.Run(data);

            Assert.That(data.Count, Is.EqualTo(5));
            Assert.That(data["firstname"], Is.EqualTo("Minnie"));
            Assert.That(data["lastname"], Is.EqualTo("Minnie"));
            Assert.That(data["email"], Is.EqualTo("mickey@mouse.com"));
        }

        [Test]
        public void SetNonExistent()
        {
            var submission = SubmissionData.Set("foo", "bar");

            var data = _data;
            submission.Run(_data);

            Assert.That(data.Count, Is.EqualTo(6));
            Assert.That(data["firstname"], Is.EqualTo("Mickey"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
            Assert.That(data["email"], Is.EqualTo("mickey@mouse.com"));
            Assert.That(data["foo"], Is.EqualTo("bar"));
        }

        [Test]
        public void Update()
        {
            var submission =
                from fn in SubmissionData.Get("firstname")
                from _ in SubmissionData.Set("firstname", fn.ToUpperInvariant()).Ignore()
                select _;

            var data = _data;
            submission.Run(data);

            Assert.That(data.Count, Is.EqualTo(5));
            Assert.That(data["firstname"], Is.EqualTo("MICKEY"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
            Assert.That(data["email"], Is.EqualTo("mickey@mouse.com"));
        }

        [Test]
        public void SetSingleWhere()
        {
            var submission =
                SubmissionData.SetSingleWhere(n => n.StartsWith("first", StringComparison.OrdinalIgnoreCase),
                                              "Minnie")
                              .Return();

            var data = _data;
            var name = submission.Run(data);

            Assert.That(name, Is.EqualTo("firstname"));
            Assert.That(data.Count, Is.EqualTo(5));
            Assert.That(data["firstname"], Is.EqualTo("Minnie"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
            Assert.That(data["email"], Is.EqualTo("mickey@mouse.com"));
        }

        [Test]
        public void SetSingleMatching()
        {
            var submission = SubmissionData.SetSingleMatching(@"^lastname$", "bar");

            var data = _data;
            submission.Run(data);

            Assert.That(data.Count, Is.EqualTo(5));
            Assert.That(data["firstname"], Is.EqualTo("Mickey"));
            Assert.That(data["lastname"], Is.EqualTo("bar"));
            Assert.That(data["email"], Is.EqualTo("mickey@mouse.com"));
        }

        [Test]
        public void SetSingleMatchingMultipleMatch()
        {
            var submission = SubmissionData.SetSingleMatching(@"^[first|last]name$", "bar");

            var data = _data;

            Assert.Throws<InvalidOperationException>(() => submission.Run(data));
            Assert.That(data.Count, Is.EqualTo(5));
            Assert.That(data["firstname"], Is.EqualTo("Mickey"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
            Assert.That(data["email"], Is.EqualTo("mickey@mouse.com"));
        }

        [Test]
        public void SetSingleMatchingNoneMatch()
        {
            var submission = SubmissionData.SetSingleMatching(@"^foo$", "bar");

            var data = _data;

            Assert.Throws<InvalidOperationException>(() => submission.Run(data));
            Assert.That(data.Count, Is.EqualTo(5));
            Assert.That(data["firstname"], Is.EqualTo("Mickey"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
            Assert.That(data["email"], Is.EqualTo("mickey@mouse.com"));
        }

        [Test]
        public void TrySetSingleWhereMultipleMatch()
        {
            var submission =
                SubmissionData.TrySetSingleWhere(n => n.EndsWith("name", StringComparison.OrdinalIgnoreCase),
                                                 "bar");
            var data = _data;

            Assert.Throws<InvalidOperationException>(() => submission.Run(data));
            Assert.That(data.Count, Is.EqualTo(5));
            Assert.That(data["firstname"], Is.EqualTo("Mickey"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
            Assert.That(data["email"], Is.EqualTo("mickey@mouse.com"));
        }

        [Test]
        public void TrySetSingleWhereNoneMatch()
        {
            var submission =
                SubmissionData.TrySetSingleWhere(n => n.StartsWith("foo", StringComparison.OrdinalIgnoreCase),
                                                 "bar")
                              .Return();

            var data = _data;
            var name = submission.Run(data);

            Assert.That(name, Is.Null);
            Assert.That(data.Count, Is.EqualTo(5));
            Assert.That(data["firstname"], Is.EqualTo("Mickey"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
            Assert.That(data["email"], Is.EqualTo("mickey@mouse.com"));
        }

        [Test]
        public void SetFirstWhere()
        {
            var submission =
                SubmissionData.SetFirstWhere(n => n.EndsWith("name", StringComparison.OrdinalIgnoreCase), "Minnie");

            var data = _data;
            submission.Run(data);

            Assert.That(data.Count, Is.EqualTo(5));
            Assert.That(data["firstname"], Is.EqualTo("Minnie"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
            Assert.That(data["email"], Is.EqualTo("mickey@mouse.com"));
        }

        [Test]
        public void SetFirstWhereNoneMatch()
        {
            var submission =
                SubmissionData.SetFirstWhere(n => n.StartsWith("foo", StringComparison.OrdinalIgnoreCase), "bar");

            var data = _data;

            Assert.Throws<InvalidOperationException>(() => submission.Run(data));
            Assert.That(data.Count, Is.EqualTo(5));
            Assert.That(data["firstname"], Is.EqualTo("Mickey"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
            Assert.That(data["email"], Is.EqualTo("mickey@mouse.com"));
        }

        [Test]
        public void SetWhere()
        {
            var submission =
                SubmissionData.SetWhere(n => n.EndsWith("name", StringComparison.OrdinalIgnoreCase), "baz");

            var data = _data;
            submission.Run(data);

            Assert.That(data.Count, Is.EqualTo(5));
            Assert.That(data["firstname"], Is.EqualTo("baz"));
            Assert.That(data["lastname"], Is.EqualTo("baz"));
            Assert.That(data["email"], Is.EqualTo("mickey@mouse.com"));
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

            var data = _data;
            submission.Run(data);

            Assert.That(data.Count, Is.EqualTo(7));
            Assert.That(data["firstname"], Is.EqualTo("Mickey"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
            Assert.That(data["email"], Is.EqualTo("mickey@mouse.com"));
            Assert.That(data["foo"], Is.EqualTo("bar"));
            Assert.That(data["bar"], Is.EqualTo("baz"));
        }

        [Test]
        public void Data()
        {
            var submission = SubmissionData.Data();
            var data = submission.Run(_data);

            Assert.That(data, Is.EqualTo(_data));
            Assert.That(data.Count, Is.EqualTo(5));
            Assert.That(data["firstname"], Is.EqualTo("Mickey"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
            Assert.That(data["email"], Is.EqualTo("mickey@mouse.com"));
        }

        [Test]
        public void Clear()
        {
            var submission = SubmissionData.Clear();
            var data = _data;
            submission.Run(data);

            Assert.That(data, Is.Empty);
        }

        [Test]
        public void Then()
        {
            var submission = SubmissionData.Set("firstname", "Minnie")
                                           .Then(SubmissionData.Set("email", "minnie@mouse.com"));

            var data = _data;
            submission.Run(data);

            Assert.That(data.Count, Is.EqualTo(5));
            Assert.That(data["firstname"], Is.EqualTo("Minnie"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
            Assert.That(data["email"], Is.EqualTo("minnie@mouse.com"));
        }

        [Test]
        public void Zip()
        {
            var submission = SubmissionData.Return(42).Zip(SubmissionData.Return('a'), (a, b) => (a, b));

            var data = _data;
            var names = submission.Run(data);

            Assert.That(names, Is.EqualTo((42, 'a')));
        }

        [Test]
        public void Collect()
        {
            var submission =
                SubmissionData.Collect(
                    SubmissionData.Set("firstname", "Minnie"),
                    SubmissionData.Set("email", "minnie@mouse.com"));

            var data = _data;
            submission.Run(data);

            Assert.That(data.Count, Is.EqualTo(5));
            Assert.That(data["firstname"], Is.EqualTo("Minnie"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
            Assert.That(data["email"], Is.EqualTo("minnie@mouse.com"));
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
            var data = _data;
            submission.Run(data);

            Assert.That(data.Count, Is.EqualTo(5));
            Assert.That(data["firstname"], Is.EqualTo("Minnie"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
            Assert.That(data["email"], Is.EqualTo("minnie@mouse.com"));
        }
    }
}
