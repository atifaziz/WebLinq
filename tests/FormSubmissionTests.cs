namespace WebLinq.Tests
{
    using System;
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
        public void Set()
        {
            var submission = FormSubmission.Set("firstname", "Minnie");

            submission.Run(_context);
            var data = _context.Data;

            Assert.That(data.Count, Is.EqualTo(2));
            Assert.That(data["firstname"], Is.EqualTo("Minnie"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
        }

        [Test]
        public void SetNonExistent()
        {
            var submission = FormSubmission.Set("foo", "bar");

            submission.Run(_context);
            var data = _context.Data;

            Assert.That(data.Count, Is.EqualTo(3));
            Assert.That(data["firstname"], Is.EqualTo("Mickey"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
            Assert.That(data["foo"], Is.EqualTo("bar"));
        }

        [Test]
        public void Update()
        {
            var submission =
                from fn in FormSubmission.Get("firstname")
                from _ in FormSubmission.Set("firstname", fn.ToUpperInvariant()).Ignore()
                select _;

            submission.Run(_context);
            var data = _context.Data;

            Assert.That(data.Count, Is.EqualTo(2));
            Assert.That(data["firstname"], Is.EqualTo("MICKEY"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
        }

        [Test]
        public void SetSingleWhere()
        {
            var submission =
                FormSubmission.SetSingleWhere(n => n.StartsWith("first", StringComparison.OrdinalIgnoreCase),
                                              "Minnie");

            var name = submission.Run(_context);
            var data = _context.Data;

            Assert.That(name, Is.EqualTo("firstname"));
            Assert.That(data.Count, Is.EqualTo(2));
            Assert.That(data["firstname"], Is.EqualTo("Minnie"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
        }

        [Test]
        public void TrySetSingleWhereNoneMatch()
        {
            var submission =
                FormSubmission.TrySetSingleWhere(n => n.StartsWith("foo", StringComparison.OrdinalIgnoreCase),
                                                 "bar");

            var name = submission.Run(_context);
            var data = _context.Data;

            Assert.That(name, Is.Null);
            Assert.That(data.Count, Is.EqualTo(2));
            Assert.That(data["firstname"], Is.EqualTo("Mickey"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
        }
    }
}
