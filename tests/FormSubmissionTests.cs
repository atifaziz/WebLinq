namespace WebLinq.Tests
{
    using System;
    using System.Collections.Specialized;
    using System.Linq;
    using NUnit.Framework;
    using static Modules.HtmlModule;

    [TestFixture]
    public class FormSubmissionTests
    {
        FormSubmissionContext _context;

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

            var form = html.Forms.Single();
            var data = form.GetSubmissionData();
            _context = new FormSubmissionContext(form, data);
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
            var submission = FormSubmission.Set("firstname", "Minnie");

            submission(_context);
            var data = _context.Data;

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
            var submission = FormSubmission.Set("foo", "bar");

            submission(_context);
            var data = _context.Data;

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
                from fn in FormSubmission.Get("firstname")
                from _ in FormSubmission.Set("firstname", fn.ToUpperInvariant()).Ignore()
                select _;

            submission(_context);
            var data = _context.Data;

            Assert.That(data.Count, Is.EqualTo(3));
            Assert.That(data["firstname"], Is.EqualTo("MICKEY"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
            Assert.That(data["email"], Is.EqualTo("mickey@mouse.com"));
        }

        [Test]
        public void SetSingleWhere()
        {
            var submission =
                FormSubmission.SetSingleWhere(n => n.StartsWith("first", StringComparison.OrdinalIgnoreCase),
                                              "Minnie");

            var name = submission(_context);
            var data = _context.Data;

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
                FormSubmission.TrySetSingleWhere(n => n.StartsWith("foo", StringComparison.OrdinalIgnoreCase),
                                                 "bar");

            var name = submission(_context);
            var data = _context.Data;

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
        public void Collect()
        {
            var submission = FormSubmission.Collect();

            var collection = submission(_context);
            var data = _context.Data;

            Assert.That(collection, Is.EqualTo(data));
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
    }
}
