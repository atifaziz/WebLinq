#region Copyright (c) 2016 Atif Aziz. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

namespace WebLinq.Tests
{
    using System;
    using NUnit.Framework;
    using Sys;
    using System.Reactive.Linq;
    using System.Collections.Generic;
    using System.Linq;
    using Mannex.Collections.Generic;

    [TestFixture]
    public class SpawnTests
    {
        enum LineKind { Output, Error }

        sealed class Spawner : ISpawner
        {
            public string Path;
            public SpawnOptions Options;
            public List<(LineKind Kind, string Line)> Return = new List<(LineKind, string)>();

            public IObservable<T> Spawn<T>(string path, SpawnOptions options, Func<string, T> stdoutSelector, Func<string, T> stderrSelector)
            {
                Path = path;
                Options = options;
                var output =
                    from e in Return
                    where e.Kind == LineKind.Output || stderrSelector != null
                    select e.Kind == LineKind.Output ? stdoutSelector(e.Line) : stderrSelector(e.Line);
                return output.ToObservable();
            }
        }

        [Test]
        public void Spawn()
        {
            var spawner = new Spawner
            {
                Return =
                {
                    (LineKind.Output, "output"),
                    (LineKind.Error, "error"),
                }
            };

            var spawn = spawner.Spawn("program", ProgramArguments.Var("foo", "bar", "baz"),
                                      s => "STDOUT: " + s.ToUpperInvariant(),
                                      s => "STDERR: " + s.ToUpperInvariant());
            var output = spawn.ToEnumerable().ToArray();

            Assert.That(spawner.Path, Is.EqualTo("program"));
            Assert.That(spawner.Options.Arguments, Is.EqualTo(new[] { "foo", "bar", "baz" }));
            Assert.That(spawner.Options, Is.SameAs(spawn.Options));
            Assert.That(output, Is.EqualTo(new[] { "STDOUT: OUTPUT", "STDERR: ERROR" }));
        }

        [Test]
        public void SpawnReturningOutputOnly()
        {
            var spawner = new Spawner
            {
                Return =
                {
                    (LineKind.Output, "output"),
                    (LineKind.Error, "error"),
                }
            };

            var output =
                spawner.Spawn("program", ProgramArguments.Var("foo", "bar", "baz"))
                       .ToEnumerable()
                       .ToArray();

            Assert.That(spawner.Path, Is.EqualTo("program"));
            Assert.That(spawner.Options.Arguments, Is.EqualTo(new[] { "foo", "bar", "baz" }));

            Assert.That(output, Is.EqualTo(new[] { "output" }));
        }

        [Test]
        public void SpawnReturningTagged()
        {
            var spawner = new Spawner
            {
                Return =
                {
                    (LineKind.Output, "output"),
                    (LineKind.Error, "error"),
                }
            };

            var output =
                spawner.Spawn("program", ProgramArguments.Var("foo", "bar", "baz"), 1, 2)
                       .ToEnumerable()
                       .ToArray();

            Assert.That(spawner.Path, Is.EqualTo("program"));
            Assert.That(spawner.Options.Arguments, Is.EqualTo(new[] { "foo", "bar", "baz" }));

            Assert.That(output, Is.EqualTo(new[]
            {
                1.AsKeyTo("output"),
                2.AsKeyTo("error")
            }));
        }

        [Test]
        public void ClearEnvironment()
        {
            var spawner = new Spawner();
            var spawn1 = spawner.Spawn("program", ProgramArguments.Var("foo", "bar", "baz"));

            Assert.That(spawn1.Options.Environment, Is.Not.Empty);

            var spawn2 = spawn1.ClearEnvironment();

            Assert.That(spawn2, Is.Not.SameAs(spawn1));
            Assert.That(spawn2.Options, Is.Not.SameAs(spawn1.Options));
            Assert.That(spawn2.Options.Environment, Is.Empty);
        }

        [Test]
        public void AddEnvironmentWithNullName()
        {
            var spawner = new Spawner();
            var spawn = spawner.Spawn("program", ProgramArguments.Var("foo", "bar", "baz"));

            var e = Assert.Throws<ArgumentNullException>(() =>
                spawn.AddEnvironment(null, string.Empty));
            Assert.That(e.ParamName, Is.EqualTo("name"));
        }

        [Test]
        public void AddEnvironmentWithEmptyName()
        {
            var spawner = new Spawner();
            var spawn = spawner.Spawn("program", ProgramArguments.Var("foo", "bar", "baz"));

            var e = Assert.Throws<ArgumentException>(() =>
                spawn.AddEnvironment(string.Empty, string.Empty));
            Assert.That(e.ParamName, Is.EqualTo("name"));
        }

        [Test]
        public void AddEnvironmentWithNullValue()
        {
            var spawner = new Spawner();
            var spawn = spawner.Spawn("program", ProgramArguments.Var("foo", "bar", "baz"));

            var e = Assert.Throws<ArgumentNullException>(() =>
                spawn.AddEnvironment("FOO", null));
            Assert.That(e.ParamName, Is.EqualTo("value"));
        }

        [Test]
        public void AddEnvironment()
        {
            var spawner = new Spawner();
            var options =
                spawner.Spawn("program", ProgramArguments.Var("foo", "bar", "baz"))
                       .ClearEnvironment()
                       .AddEnvironment("FOO", "BAR")
                       .Options;

            Assert.That(options.Environment, Is.EqualTo(new[]
            {
                KeyValuePair.Create("FOO", "BAR")
            }));

            options = options.AddEnvironment("FOO", "BAZ");

            Assert.That(options.Environment, Is.EqualTo(new[]
            {
                KeyValuePair.Create("FOO", "BAR"),
                KeyValuePair.Create("FOO", "BAZ"),
            }));
        }

        [Test]
        public void SetEnvironmentWithNullName()
        {
            var spawner = new Spawner();
            var spawn = spawner.Spawn("program", ProgramArguments.Var("foo", "bar", "baz"));

            var e = Assert.Throws<ArgumentNullException>(() =>
                spawn.SetEnvironment(null, string.Empty));
            Assert.That(e.ParamName, Is.EqualTo("name"));
        }

        [Test]
        public void SetEnvironmentWithNullValueUnsets()
        {
            var spawner = new Spawner();
            var spawn = spawner.Spawn("program", ProgramArguments.Var("foo", "bar", "baz"));

            var options = spawn.ClearEnvironment()
                               .AddEnvironment("FOO", "BAR")
                               .AddEnvironment("FOO", "BAZ")
                               .Options;

            Assert.That(options.Environment, Is.EqualTo(new[]
            {
                KeyValuePair.Create("FOO", "BAR"),
                KeyValuePair.Create("FOO", "BAZ"),
            }));

            options = options.SetEnvironment("FOO", null);

            Assert.That(options.Environment, Is.Empty);
        }

        [Test]
        public void UnsetEnvironmentWithNullName()
        {
            var spawner = new Spawner();
            var spawn = spawner.Spawn("program", ProgramArguments.Var("foo", "bar", "baz"));

            var e = Assert.Throws<ArgumentNullException>(() =>
                spawn.UnsetEnvironment(null));

            Assert.That(e.ParamName, Is.EqualTo("name"));
        }

        [Test]
        public void UnsetEnvironmentWithEmptyName()
        {
            var spawner = new Spawner();
            var spawn = spawner.Spawn("program", ProgramArguments.Var("foo", "bar", "baz"));

            var e = Assert.Throws<ArgumentException>(() =>
                spawn.UnsetEnvironment(string.Empty));
            Assert.That(e.ParamName, Is.EqualTo("name"));
        }

        [Test]
        public void UnsetEnvironment()
        {
            var spawner = new Spawner();
            var options = spawner.Spawn("program", ProgramArguments.Var("foo", "bar", "baz"))
                                 .ClearEnvironment()
                                 .AddEnvironment("FOO", "BAR")
                                 .AddEnvironment("FOO", "BAZ")
                                 .Options;

            Assert.That(options.Environment, Is.EqualTo(new[]
            {
                KeyValuePair.Create("FOO", "BAR"),
                KeyValuePair.Create("FOO", "BAZ"),
            }));

            options = options.UnsetEnvironment("FOO");
            Assert.That(options.Environment, Is.Empty);
        }
    }
}
