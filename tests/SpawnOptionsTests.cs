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
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using NUnit.Framework;
    using NUnit.Framework.Constraints;
    using Optuple;
    using Sys;
    using static Optuple.OptionModule;

    [TestFixture]
    public class SpawnOptionsTests
    {
        [Test]
        public void CreateInheritsWorkingDirectoryAndEnvironment()
        {
            var options = SpawnOptions.Create();

            Assert.That(options.WorkingDirectory, Is.EqualTo(Environment.CurrentDirectory));

            var env =
                from DictionaryEntry e in Environment.GetEnvironmentVariables()
                select KeyValuePair.Create(e.Key, e.Value);

            Assert.That(options.Environment, Is.EqualTo(env));
        }

        [Test]
        public void UpdateWithNullThis()
        {
            var e = Assert.Throws<ArgumentNullException>(() =>
                SpawnOptionsExtensions.Update(null, null));
            Assert.That(e.ParamName, Is.EqualTo("options"));
        }

        [Test]
        public void UpdateWithNullProcessStartInfo()
        {
            var e = Assert.Throws<ArgumentNullException>(() =>
                SpawnOptions.Create().Update(null));
            Assert.That(e.ParamName, Is.EqualTo("startInfo"));
        }

        [Test]
        public void UpdateProcessStartInfo()
        {
            var options = SpawnOptions.Create()
                                      .ClearEnvironment()
                                      .WithWorkingDirectory(Environment.GetEnvironmentVariable("PATH")
                                                                       .Split(Path.PathSeparator)
                                                                       .First());

            var psi = new ProcessStartInfo();

            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            var windowStyle             = psi.WindowStyle;
            var verb                    = psi.Verb;
            var errorDialogParentHandle = psi.ErrorDialogParentHandle;
            var errorDialog             = psi.ErrorDialog;
            var useShellExecute         = psi.UseShellExecute;
            var userName                = psi.UserName;
            var standardOutputEncoding  = psi.StandardOutputEncoding;
            var standardInputEncoding   = psi.StandardInputEncoding;
            var standardErrorEncoding   = psi.StandardErrorEncoding;
            var redirectStandardOutput  = psi.RedirectStandardOutput;
            var redirectStandardInput   = psi.RedirectStandardInput;
            var redirectStandardError   = psi.RedirectStandardError;
            var password                = isWindows ? Some(psi.Password) : default;
            var loadUserProfile         = isWindows ? Some(psi.LoadUserProfile) : default;
            var fileName                = psi.FileName;
            var domain                  = isWindows ? Some(psi.Domain) : default;
            var createNoWindow          = psi.CreateNoWindow;
            var argumentList            = psi.ArgumentList;
            var arguments               = psi.Arguments;
            var passwordInClearText     = psi.PasswordInClearText;

            options.Update(psi);

            Assert.That(psi.WorkingDirectory, Is.SameAs(options.WorkingDirectory));
            Assert.That(psi.Environment, Is.EqualTo(options.Environment));

            Assert.That(psi.WindowStyle                , Is.EqualTo(windowStyle            ));
            Assert.That(psi.Verb                       , Is.SameAs (verb                   ));
            Assert.That(psi.ErrorDialogParentHandle    , Is.EqualTo(errorDialogParentHandle));
            Assert.That(psi.ErrorDialog                , Is.EqualTo(errorDialog            ));
            Assert.That(psi.UseShellExecute            , Is.EqualTo(useShellExecute        ));
            Assert.That(psi.UserName                   , Is.SameAs (userName               ));
            Assert.That(psi.StandardOutputEncoding     , Is.SameAs (standardOutputEncoding ));
            Assert.That(psi.StandardInputEncoding      , Is.SameAs (standardInputEncoding  ));
            Assert.That(psi.StandardErrorEncoding      , Is.SameAs (standardErrorEncoding  ));
            Assert.That(psi.RedirectStandardOutput     , Is.EqualTo(redirectStandardOutput ));
            Assert.That(psi.RedirectStandardInput      , Is.EqualTo(redirectStandardInput  ));
            Assert.That(psi.RedirectStandardError      , Is.EqualTo(redirectStandardError  ));
            Assert.That(psi.FileName                   , Is.SameAs (fileName               ));
            Assert.That(psi.CreateNoWindow             , Is.EqualTo(createNoWindow         ));
            Assert.That(psi.ArgumentList               , Is.SameAs (argumentList           ));
            Assert.That(psi.Arguments                  , Is.EqualTo(arguments              ));
            Assert.That(psi.PasswordInClearText        , Is.SameAs (passwordInClearText    ));

            AssertThat(() => psi.Password       , password       , Is.SameAs);
            AssertThat(() => psi.LoadUserProfile, loadUserProfile, v => Is.EqualTo(v));
            AssertThat(() => psi.Domain         , domain         , Is.SameAs);

            void AssertThat<T>(Func<T> actual, (bool, T) option, Func<T, IResolveConstraint> expression) =>
                option.Do(v => Assert.That(actual(), expression(v)));
        }

        [Test]
        public void ChangeWorkingDirectory()
        {
            var options = SpawnOptions.Create();
            var old = options.WorkingDirectory;
            var @new = old.ToUpperInvariant();
            var updated = options.WithWorkingDirectory(@new);

            Assert.That(updated.WorkingDirectory, Is.Not.EqualTo(old));
            Assert.That(updated.WorkingDirectory, Is.SameAs(@new));
        }

        [Test]
        public void ChangeEnvironment()
        {
            var options = SpawnOptions.Create();
            var old = options.Environment;
            var updated = options.WithEnvironment(old.Add(KeyValuePair.Create("FOO", "BAR")));

            Assert.That(updated, Is.Not.EqualTo(old));
        }

        [Test]
        public void SameWorkingDirectoryReturnsSameOptions()
        {
            var options = SpawnOptions.Create();

            Assert.That(options.WithWorkingDirectory(options.WorkingDirectory),
                        Is.SameAs(options));
        }

        [Test]
        public void SameEmptyEnvironmentReturnsSameOptions()
        {
            var empty = ImmutableArray<KeyValuePair<string, string>>.Empty;
            var options = SpawnOptions.Create().WithEnvironment(empty);

            Assert.That(options.Environment, Is.Empty);
            Assert.That(options.WithEnvironment(empty), Is.SameAs(options));
        }

        [Test]
        public void ClearEnvironmentWithNullThis()
        {
            var e = Assert.Throws<ArgumentNullException>(() =>
                SpawnOptionsExtensions.ClearEnvironment(null));
            Assert.That(e.ParamName, Is.EqualTo("options"));
        }

        [Test]
        public void ClearEnvironment()
        {
            var options = SpawnOptions.Create();

            Assert.That(options.Environment, Is.Not.Empty);

            var updated = options.ClearEnvironment();

            Assert.That(updated, Is.Not.SameAs(options));
            Assert.That(updated.Environment, Is.Empty);
        }

        [Test]
        public void AddEnvironmentWithNullThis()
        {
            var e = Assert.Throws<ArgumentNullException>(() =>
                SpawnOptionsExtensions.AddEnvironment(null, null, null));
            Assert.That(e.ParamName, Is.EqualTo("options"));
        }

        [Test]
        public void AddEnvironmentWithNullName()
        {
            var e = Assert.Throws<ArgumentNullException>(() =>
                SpawnOptions.Create().AddEnvironment(null, string.Empty));
            Assert.That(e.ParamName, Is.EqualTo("name"));
        }

        [Test]
        public void AddEnvironmentWithEmptyName()
        {
            var e = Assert.Throws<ArgumentException>(() =>
                SpawnOptions.Create().AddEnvironment(string.Empty, string.Empty));
            Assert.That(e.ParamName, Is.EqualTo("name"));
        }

        [Test]
        public void AddEnvironmentWithNullValue()
        {
            var e = Assert.Throws<ArgumentNullException>(() =>
                SpawnOptions.Create().AddEnvironment("FOO", null));
            Assert.That(e.ParamName, Is.EqualTo("value"));
        }

        [Test]
        public void AddEnvironment()
        {
            var options = SpawnOptions.Create()
                                      .ClearEnvironment()
                                      .AddEnvironment("FOO", "BAR");

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
        public void SetEnvironmentWithNullThis()
        {
            var e = Assert.Throws<ArgumentNullException>(() =>
                SpawnOptionsExtensions.SetEnvironment(null, null, null));
            Assert.That(e.ParamName, Is.EqualTo("options"));
        }

        [Test]
        public void SetEnvironmentWithNullName()
        {
            var e = Assert.Throws<ArgumentNullException>(() =>
                SpawnOptions.Create().SetEnvironment(null, string.Empty));
            Assert.That(e.ParamName, Is.EqualTo("name"));
        }

        [Test]
        public void SetEnvironmentWithNullValueUnsets()
        {
            var options = SpawnOptions.Create()
                                      .ClearEnvironment()
                                      .AddEnvironment("FOO", "BAR")
                                      .AddEnvironment("FOO", "BAZ");

            Assert.That(options.Environment, Is.EqualTo(new[]
            {
                KeyValuePair.Create("FOO", "BAR"),
                KeyValuePair.Create("FOO", "BAZ"),
            }));

            options = options.SetEnvironment("FOO", null);
            Assert.That(options.Environment, Is.Empty);
        }

        [Test]
        public void UnsetEnvironmentWithNullThis()
        {
            var e = Assert.Throws<ArgumentNullException>(() =>
                SpawnOptionsExtensions.UnsetEnvironment(null, null));
            Assert.That(e.ParamName, Is.EqualTo("options"));
        }

        [Test]
        public void UnsetEnvironmentWithNullName()
        {
            var e = Assert.Throws<ArgumentNullException>(() =>
                SpawnOptions.Create().UnsetEnvironment(null));
            Assert.That(e.ParamName, Is.EqualTo("name"));
        }

        [Test]
        public void UnsetEnvironmentWithEmptyName()
        {
            var e = Assert.Throws<ArgumentException>(() =>
                SpawnOptions.Create().UnsetEnvironment(string.Empty));
            Assert.That(e.ParamName, Is.EqualTo("name"));
        }

        [Test]
        public void UnsetEnvironment()
        {
            var options = SpawnOptions.Create()
                                       .ClearEnvironment()
                                       .AddEnvironment("FOO", "BAR")
                                       .AddEnvironment("FOO", "BAZ");

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
