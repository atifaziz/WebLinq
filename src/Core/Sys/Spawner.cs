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
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using Mannex;
    using Mannex.Collections.Generic;
    using Mannex.Diagnostics;

    #endregion

    public interface ISpawnObservable<out T> : IObservable<T>
    {
        SpawnOptions Options { get; }
        ISpawnObservable<T> WithOptions(SpawnOptions options);
    }

    static class SpawnObservable
    {
        public static ISpawnObservable<T> Create<T>(SpawnOptions options, Func<IObserver<T>, SpawnOptions, IDisposable> subscriber) =>
            new Implementation<T>(options, subscriber);

        sealed class Implementation<T> : ISpawnObservable<T>
        {
            readonly Func<IObserver<T>, SpawnOptions, IDisposable> _subscriber;

            public Implementation(SpawnOptions options, Func<IObserver<T>, SpawnOptions, IDisposable> subscriber)
            {
                Options = options ?? throw new ArgumentNullException(nameof(options));
                _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
            }

            public SpawnOptions Options { get; }

            public ISpawnObservable<T> WithOptions(SpawnOptions value) =>
                ReferenceEquals(Options, value) ? this : Create(value, _subscriber);

            public IDisposable Subscribe(IObserver<T> observer) =>
                _subscriber(observer, Options);
        }
    }

    public interface ISpawner
    {
        IObservable<T> Spawn<T>(string path, SpawnOptions options, Func<string, T> stdoutSelector, Func<string, T> stderrSelector);
    }

    public static class SpawnerExtensions
    {
        public static ISpawnObservable<T> AddArgument<T>(this ISpawnObservable<T> source, string value) =>
            source.WithOptions(source.Options.AddArgument(value));

        public static ISpawnObservable<T> AddArgument<T>(this ISpawnObservable<T> source, params string[] values) =>
            source.WithOptions(source.Options.AddArgument(values));

        public static ISpawnObservable<T> AddArguments<T>(this ISpawnObservable<T> source, IEnumerable<string> values) =>
            source.WithOptions(source.Options.AddArguments(values));

        public static ISpawnObservable<T> ClearArguments<T>(this ISpawnObservable<T> source) =>
            source.WithOptions(source.Options.ClearArguments());

        public static ISpawnObservable<T> SetCommandLine<T>(this ISpawnObservable<T> source, string value) =>
            source.WithOptions(source.Options.SetCommandLine(value));

        public static ISpawnObservable<T> ClearEnvironment<T>(this ISpawnObservable<T> source) =>
            source.WithOptions(source.Options.ClearEnvironment());

        public static ISpawnObservable<T> AddEnvironment<T>(this ISpawnObservable<T> source, string name, string value) =>
            source.WithOptions(source.Options.AddEnvironment(name, value));

        public static ISpawnObservable<T> SetEnvironment<T>(this ISpawnObservable<T> source, string name, string value) =>
            source.WithOptions(source.Options.SetEnvironment(name, value));

        public static ISpawnObservable<T> UnsetEnvironment<T>(this ISpawnObservable<T> source, string name) =>
            source.WithOptions(source.Options.UnsetEnvironment(name));

        public static ISpawnObservable<T> WorkingDirectory<T>(this ISpawnObservable<T> source, string value) =>
            source.WithOptions(source.Options.WithWorkingDirectory(value));

        public static ISpawnObservable<string> Spawn(this ISpawner spawner, string path, ProgramArguments args) =>
            spawner.Spawn(path, args, output => output, null);

        public static ISpawnObservable<KeyValuePair<T, string>>
            Spawn<T>(this ISpawner spawner,
                     string path, ProgramArguments args,
                     T stdoutKey, T stderrKey) =>
            spawner.Spawn(path, args, stdout => stdoutKey.AsKeyTo(stdout),
                                      stderr => stderrKey.AsKeyTo(stderr));

        public static ISpawnObservable<T> Spawn<T>(this ISpawner spawner, string path, ProgramArguments args, Func<string, T> stdoutSelector, Func<string, T> stderrSelector) =>
            SpawnObservable.Create<T>(SpawnOptions.Create().WithArguments(args),
                (observer, options) =>
                    spawner.Spawn(path, options, stdoutSelector, stderrSelector).Subscribe(observer));
    }

    public static class Spawner
    {
        public static ISpawner Default => new SysSpawner();

        sealed class SysSpawner : ISpawner
        {
            public IObservable<T> Spawn<T>(string path, SpawnOptions options, Func<string, T> stdoutSelector, Func<string, T> stderrSelector) =>
                SpawnCore(path, options, stdoutSelector, stderrSelector).ToObservable();
        }

        // TODO Make true observable
        static IEnumerable<T> SpawnCore<T>(string path, SpawnOptions options, Func<string, T> stdoutSelector, Func<string, T> stderrSelector)
        {
            var psi = new ProcessStartInfo(path, options.Arguments.ToString())
            {
                CreateNoWindow         = true,
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
            };

            options.Update(psi);

            using (var process = Process.Start(psi))
            {
                Debug.Assert(process != null);

                var bc = new BlockingCollection<Tuple<T, Exception>>();
                var drainer = process.BeginReadLineAsync(stdoutSelector != null ? (stdout => bc.Add(Tuple.Create(stdoutSelector(stdout), default(Exception)))) : (Action<string>)null,
                                                         stderrSelector != null ? (stderr => bc.Add(Tuple.Create(stderrSelector(stderr), default(Exception)))) : (Action<string>)null);

                Task.Run(async () => // ReSharper disable AccessToDisposedClosure
                {
                    try
                    {
                        var pid = process.Id;
                        var error = await
                            process.AsTask(dispose: false,
                                           p => p.ExitCode != 0 ? new Exception($"Process \"{Path.GetFileName(path)}\" (launched as the ID {pid}) ended with the non-zero exit code {p.ExitCode}.")
                                                                : null,
                                           e => e,
                                           e => e)
                                   .DontContinueOnCapturedContext();

                        await drainer(null).DontContinueOnCapturedContext();

                        if (error != null)
                            throw error;

                        bc.CompleteAdding();
                    }
                    catch (Exception e)
                    {
                        bc.Add(Tuple.Create(default(T), e));
                    }

                    // ReSharper restore AccessToDisposedClosure
                });

                foreach (var e in from e in bc.GetConsumingEnumerable()
                                  select e.Fold((res, err) => new { Result = res, Error = err }))
                {
                    if (e.Error != null)
                        throw e.Error;
                    yield return e.Result;
                }
            }
        }
    }
}
