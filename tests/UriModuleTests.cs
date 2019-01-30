namespace WebLinq.Tests
{
    using System;
    using NUnit.Framework;
    using Modules;

    [TestFixture]
    public class UriModuleTests
    {
        [Test]
        public void FormatUri()
        {
            var date = new DateTime(2007, 6, 29);
            var url = UriModule.FormatUri($@"
                http://www.example.com/
                    {date:yyyy}/
                    {date:MM}/
                    {date:dd}/
                    {{123_456_789}}/
                    {123_456_789}/
                    info.html
                    ?h={"foo bar"}
                    &date={date:MMM dd, yyyy}");

            Assert.That(url.AbsoluteUri,
                Is.EqualTo("http://www.example.com/"
                         + "2007/"
                         + "06/"
                         + "29/"
                         + "%7B123_456_789%7D/"
                         + "123456789/"
                         + "info.html"
                         + "?h=foo%20bar"
                         + "&date=Jun%2029%2C%202007"));
        }
    }
}
