namespace WebLinq.Tests
{
    using System;
    using System.Linq;
    using System.Reactive.Linq;
    using NUnit.Framework;
    using Text;
    using static MoreLinq.Extensions.TakeUntilExtension;

    public class TextQueryTests
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void LinesTake(int count)
        {
            var lines = new[] { "foo", "bar", "baz", "qux" };

            var tt = new TestTransport().EnqueueText(string.Join(Environment.NewLine, lines));

            var result =
                tt.Http.Get(new Uri("http://www.example.com/"))
                    .Lines()
                    .Take(count)
                    .ToEnumerable();

            Assert.That(result, Is.EqualTo(lines.Take(count)));
        }

        [TestCase("foo")]
        [TestCase("bar")]
        [TestCase("baz")]
        [TestCase("qux")]
        [TestCase("")]
        public void LinesTakeWhile(string stop)
        {
            var lines = new[] { "foo", "bar", "baz", "qux" };

            var tt = new TestTransport().EnqueueText(string.Join(Environment.NewLine, lines));

            var result =
                tt.Http.Get(new Uri("http://www.example.com/"))
                    .Lines()
                    .TakeWhile((s, _) => s != stop)
                    .ToEnumerable();

            Assert.That(result, Is.EqualTo(lines.TakeWhile(s => s != stop)));
        }

        [TestCase("foo")]
        [TestCase("bar")]
        [TestCase("baz")]
        [TestCase("qux")]
        [TestCase("")]
        public void LinesTakeUntil(string stop)
        {
            var lines = new[] { "foo", "bar", "baz", "qux" };

            var tt = new TestTransport().EnqueueText(string.Join(Environment.NewLine, lines));

            var result =
                tt.Http.Get(new Uri("http://www.example.com/"))
                    .Lines()
                    .TakeUntil(s => s == stop)
                    .ToEnumerable();

            Assert.That(result, Is.EqualTo(lines.TakeUntil(s => s == stop)));
        }
    }
}
