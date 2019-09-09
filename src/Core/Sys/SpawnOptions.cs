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

namespace WebLinq.Sys
{
    #region Imports

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.InteropServices;
    using Mannex.Collections.Generic;

    #endregion

    public sealed class SpawnOptions
    {
        public static SpawnOptions Create() =>
            new SpawnOptions(System.Environment.CurrentDirectory,
                             ImmutableArray.CreateRange(from DictionaryEntry e in System.Environment.GetEnvironmentVariables()
                                                        select ((string)e.Key).AsKeyTo((string)e.Value)));

        SpawnOptions(string workingDirectory, ImmutableArray<KeyValuePair<string, string>> environment)
        {
            WorkingDirectory = workingDirectory;
            Environment = environment;
        }

        public string WorkingDirectory { get; }
        public ImmutableArray<KeyValuePair<string, string>> Environment { get; }

        public SpawnOptions WithEnvironment(ImmutableArray<KeyValuePair<string, string>> value) =>
            Environment.IsEmpty && value.IsEmpty ? this : new SpawnOptions(WorkingDirectory, value);

        public SpawnOptions WithWorkingDirectory(string value)
            => value is null ? throw new ArgumentNullException(nameof(value))
             : value == WorkingDirectory ? this
             : new SpawnOptions(value, Environment);
    }

    public static class SpawnOptionsExtensions
    {
        public static SpawnOptions AddEnvironment(this SpawnOptions options, string name, string value)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));
            if (name is null) throw new ArgumentNullException(nameof(name));
            if (name.Length == 0) throw new ArgumentException(null, nameof(name));
            if (value is null) throw new ArgumentNullException(nameof(value));

            return options.WithEnvironment(options.Environment.Add(name.AsKeyTo(value)));
        }

        public static SpawnOptions ClearEnvironment(this SpawnOptions options)
            => options is null ? throw new ArgumentNullException(nameof(options))
             : options.WithEnvironment(ImmutableArray<KeyValuePair<string, string>>.Empty);

        public static SpawnOptions SetEnvironment(this SpawnOptions options, string name, string value)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));
            var updateOptions = options.UnsetEnvironment(name);
            return value is null ? updateOptions : updateOptions.AddEnvironment(name, value);
        }

        public static SpawnOptions UnsetEnvironment(this SpawnOptions options, string name)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));
            if (name is null) throw new ArgumentNullException(nameof(name));
            if (name.Length == 0) throw new ArgumentException(null, nameof(name));

            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var comparison = isWindows ? StringComparison.OrdinalIgnoreCase
                                       : StringComparison.Ordinal;

            var found = false;
            foreach (var e in options.Environment)
            {
                if (found = string.Equals(e.Key, name, comparison))
                    break;
            }

            return found
                 ? options.WithEnvironment(ImmutableArray.CreateRange(
                       from e in options.Environment
                       where !string.Equals(e.Key, name, comparison)
                       select e))
                 : options;
        }

        public static void Update(this SpawnOptions options, ProcessStartInfo startInfo)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));
            if (startInfo is null) throw new ArgumentNullException(nameof(startInfo));

            startInfo.WorkingDirectory = options.WorkingDirectory;

            var environment = startInfo.Environment;
            environment.Clear();
            foreach (var e in options.Environment)
                environment.Add(e.Key, e.Value);
        }
    }
}
