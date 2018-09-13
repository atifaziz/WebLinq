namespace WebLinq.Tests
{
    using System;
    using System.Collections.Specialized;
    using System.Linq;
    using Html;
    using NUnit.Framework;
    using static Modules.HtmlModule;

    [TestFixture]
    public class FormSubmissionTests
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
                    <input type='submit' value='Submit' >
                  </form>
                </body>
                </html>");

            _data = html.Forms.Single().GetSubmissionData();
        }

        [Test]
        public void Return()
        {
            var submission = FormSubmission.Return("foo");
            var value = submission(_context);

            Assert.That(value, Is.EqualTo("foo"));
        }

        [Test]
        public void Select()
        {
            var submission =
                from n in FormSubmission.Return(42)
                select new string((char) n, n);

            var names = submission(_context);
            Assert.That(names, Is.EqualTo(new string('*', 42)));
        }

        [Test]
        public void SelectMany()
        {
            var submission =
                from n in FormSubmission.Return(42)
                from s in FormSubmission.Return(new string((char) n, n))
                select string.Concat(n, s);

            var names = submission(_context);
            var stars = new string('*', 42);

            Assert.That(names, Is.EqualTo("42" + stars));
        }

        [Test]
        public void For()
        {
            var source = new[] { 3, 4, 5 };
            var submission = FormSubmission.For(source, e => FormSubmission.Return(e*3));

            var values = submission(_context);
            Assert.That(values, Is.EqualTo(new[] { 9, 12, 15 }));
        }

        [Test]
        public void Names()
        {
            var submission = FormSubmission.Names();
            var names = submission(_context);

            var data = _context.Data;

            Assert.That(names, Is.EqualTo(new [] {"firstname","lastname","email"}));
            Assert.That(data["firstname"], Is.EqualTo("Mickey"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
            Assert.That(data["email"], Is.EqualTo("mickey@mouse.com"));
        }

        [Test]
        public void Get()
        {
            var submission1 = FormSubmission.Get("firstname");
            var value1 = submission1(_context);
            var submission2 = FormSubmission.Get("lastname");
            var value2 = submission2(_context);
            var submission3 = FormSubmission.Get("email");
            var value3 = submission3(_context);

            Assert.That(value1, Is.EqualTo("Mickey"));
            Assert.That(value2, Is.EqualTo("Mouse"));
            Assert.That(value3, Is.EqualTo("mickey@mouse.com"));
        }

        [Test]
        public void Set()
        {
            var submission = SubmissionData.Set("firstname", "Minnie");

            var data = _data;
            submission.Run(data);

            Assert.That(data.Count, Is.EqualTo(3));
            Assert.That(data["firstname"], Is.EqualTo("Minnie"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
            Assert.That(data["email"], Is.EqualTo("mickey@mouse.com"));
        }

        [Test]
        public void SetNames()
        {
            var submission = FormSubmission.Set(new[] { "firstname", "lastname" }, "Minnie");

            submission(_context);
            var data = _context.Data;

            Assert.That(data.Count, Is.EqualTo(3));
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

            Assert.That(data.Count, Is.EqualTo(4));
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

            Assert.That(data.Count, Is.EqualTo(3));
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
            Assert.That(data.Count, Is.EqualTo(3));
            Assert.That(data["firstname"], Is.EqualTo("Minnie"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
            Assert.That(data["email"], Is.EqualTo("mickey@mouse.com"));
        }

        [Test]
        public void SetSingleMatching()
        {
            var submission = FormSubmission.SetSingleMatching(@"^lastname$", "bar");

            submission(_context);
            var data = _context.Data;

            Assert.That(data.Count, Is.EqualTo(3));
            Assert.That(data["firstname"], Is.EqualTo("Mickey"));
            Assert.That(data["lastname"], Is.EqualTo("bar"));
            Assert.That(data["email"], Is.EqualTo("mickey@mouse.com"));
        }

        [Test]
        public void SetSingleMatchingMultipleMatch()
        {
            var submission = FormSubmission.SetSingleMatching(@"^[first|last]name$", "bar");

            var data = _context.Data;

            Assert.Throws<InvalidOperationException>(() => submission(_context));
            Assert.That(data.Count, Is.EqualTo(3));
            Assert.That(data["firstname"], Is.EqualTo("Mickey"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
            Assert.That(data["email"], Is.EqualTo("mickey@mouse.com"));
        }

        [Test]
        public void SetSingleMatchingNoneMatch()
        {
            var submission = FormSubmission.SetSingleMatching(@"^foo$", "bar");

            var data = _context.Data;

            Assert.Throws<InvalidOperationException>(() => submission(_context));
            Assert.That(data.Count, Is.EqualTo(3));
            Assert.That(data["firstname"], Is.EqualTo("Mickey"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
            Assert.That(data["email"], Is.EqualTo("mickey@mouse.com"));
        }

        [Test]
        public void TrySetSingleWhereMultipleMatch()
        {
            var submission =
                FormSubmission.TrySetSingleWhere(n => n.EndsWith("name", StringComparison.OrdinalIgnoreCase),
                                                 "bar");
            var data = _context.Data;

            Assert.Throws<InvalidOperationException>(() => submission(_context));
            Assert.That(data.Count, Is.EqualTo(3));
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
            Assert.That(data.Count, Is.EqualTo(3));
            Assert.That(data["firstname"], Is.EqualTo("Mickey"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
            Assert.That(data["email"], Is.EqualTo("mickey@mouse.com"));
        }

        [Test]
        public void SetFirstWhere()
        {
            var submission =
                FormSubmission.SetFirstWhere(n => n.EndsWith("name", StringComparison.OrdinalIgnoreCase), "Minnie");

            submission(_context);
            var data = _context.Data;

            Assert.That(data.Count, Is.EqualTo(3));
            Assert.That(data["firstname"], Is.EqualTo("Minnie"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
            Assert.That(data["email"], Is.EqualTo("mickey@mouse.com"));
        }

        [Test]
        public void SetFirstWhereNoneMatch()
        {
            var submission =
                FormSubmission.SetFirstWhere(n => n.StartsWith("foo", StringComparison.OrdinalIgnoreCase), "bar");

            var data = _context.Data;

            Assert.Throws<InvalidOperationException>(() => submission(_context));
            Assert.That(data.Count, Is.EqualTo(3));
            Assert.That(data["firstname"], Is.EqualTo("Mickey"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
            Assert.That(data["email"], Is.EqualTo("mickey@mouse.com"));
        }

        [Test]
        public void SetWhere()
        {
            var submission =
                FormSubmission.SetWhere(n => n.EndsWith("name", StringComparison.OrdinalIgnoreCase), "baz");

            submission(_context);
            var data = _context.Data;

            Assert.That(data.Count, Is.EqualTo(3));
            Assert.That(data["firstname"], Is.EqualTo("baz"));
            Assert.That(data["lastname"], Is.EqualTo("baz"));
            Assert.That(data["email"], Is.EqualTo("mickey@mouse.com"));
        }

        [Test]
        public void Merge()
        {
            var other = new NameValueCollection() {
                ["foo"] = "bar",
                ["bar"] = "baz"
            };

            var submission = FormSubmission.Merge(other);

            submission(_context);
            var data = _context.Data;

            Assert.That(data.Count, Is.EqualTo(5));
            Assert.That(data["firstname"], Is.EqualTo("Mickey"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
            Assert.That(data["email"], Is.EqualTo("mickey@mouse.com"));
            Assert.That(data["foo"], Is.EqualTo("bar"));
            Assert.That(data["bar"], Is.EqualTo("baz"));
        }

        [Test]
        public void Data()
        {
            var submission = FormSubmission.Data();
            var data = submission(_context);

            Assert.That(data, Is.EqualTo(_context.Data));
            Assert.That(data.Count, Is.EqualTo(3));
            Assert.That(data["firstname"], Is.EqualTo("Mickey"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
            Assert.That(data["email"], Is.EqualTo("mickey@mouse.com"));
        }

        [Test]
        public void Clear()
        {
            var submission = FormSubmission.Clear();
            submission(_context);

            var data = _context.Data;

            Assert.That(data, Is.Empty);
        }

        [Test]
        public void Then()
        {
            var submission = FormSubmission.Set("firstname", "Minnie")
                                           .Then(FormSubmission.Set("email", "minnie@mouse.com"));

            submission(_context);
            var data = _context.Data;

            Assert.That(data.Count, Is.EqualTo(3));
            Assert.That(data["firstname"], Is.EqualTo("Minnie"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
            Assert.That(data["email"], Is.EqualTo("minnie@mouse.com"));
        }

        [Test]
        public void Collect()
        {
            var submission =
                FormSubmission.Collect(
                    FormSubmission.Set("firstname", "Minnie"),
                    FormSubmission.Set("email", "minnie@mouse.com"));

            submission(_context);

            var data = _context.Data;

            Assert.That(data.Count, Is.EqualTo(3));
            Assert.That(data["firstname"], Is.EqualTo("Minnie"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
            Assert.That(data["email"], Is.EqualTo("minnie@mouse.com"));
        }

        [Test]
        public void CollectAsEnumerable()
        {
            var sets = new[]
            {
                FormSubmission.Set("firstname", "Minnie"),
                FormSubmission.Set("email", "minnie@mouse.com"),
            };
            var submission = FormSubmission.Collect(sets.AsEnumerable());
            submission(_context);

            var data = _context.Data;

            Assert.That(data.Count, Is.EqualTo(3));
            Assert.That(data["firstname"], Is.EqualTo("Minnie"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
            Assert.That(data["email"], Is.EqualTo("minnie@mouse.com"));
        }
    }
}
