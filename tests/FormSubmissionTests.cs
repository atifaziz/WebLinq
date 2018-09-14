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
                    <br><br>
                    <input type='submit' value='Submit' >
                  </form>
                </body>
                </html>");

            _data = html.Forms.Single().GetSubmissionData();
        }

        [Test]
        public void Set()
        {
            var submission = SubmissionData.Set("firstname", "Minnie");

            var data = _data;
            submission.Run(data);

            Assert.That(data.Count, Is.EqualTo(2));
            Assert.That(data["firstname"], Is.EqualTo("Minnie"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
        }

        [Test]
        public void SetNonExistent()
        {
            var submission = SubmissionData.Set("foo", "bar");

            var data = _data;
            submission.Run(_data);

            Assert.That(data.Count, Is.EqualTo(3));
            Assert.That(data["firstname"], Is.EqualTo("Mickey"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
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

            Assert.That(data.Count, Is.EqualTo(2));
            Assert.That(data["firstname"], Is.EqualTo("MICKEY"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
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
            Assert.That(data.Count, Is.EqualTo(2));
            Assert.That(data["firstname"], Is.EqualTo("Minnie"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
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
            Assert.That(data.Count, Is.EqualTo(2));
            Assert.That(data["firstname"], Is.EqualTo("Mickey"));
            Assert.That(data["lastname"], Is.EqualTo("Mouse"));
        }
    }
}
