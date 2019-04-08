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

    public interface ISpawner
    {
        IObservable<T> Spawn<T>(string path, ProgramArguments args, string workingDirectory, Func<string, T> stdoutSelector, Func<string, T> stderrSelector);
    }

    public static class SpawnerExtensions
    {
        public static IObservable<string> Spawn(this ISpawner spawner, string path, ProgramArguments args) =>
            spawner.Spawn(path, args, null);

        public static IObservable<string> Spawn(this ISpawner spawner, string path, ProgramArguments args, string workingDirectory) =>
            spawner.Spawn(path, args, workingDirectory, output => output, null);

        public static IObservable<KeyValuePair<T, string>> Spawn<T>(this ISpawner spawner, string path, ProgramArguments args, T stdoutKey, T stderrKey) =>
            spawner.Spawn(path, args, null, stdoutKey, stderrKey);

        public static IObservable<KeyValuePair<T, string>> Spawn<T>(this ISpawner spawner, string path, ProgramArguments args, string workingDirectory, T stdoutKey, T stderrKey) =>
            spawner.Spawn(path, args, workingDirectory,
                               stdout => stdoutKey.AsKeyTo(stdout),
                               stderr => stderrKey.AsKeyTo(stderr));

        public static IObservable<T> Spawn<T>(this ISpawner spawner, string path, ProgramArguments args, Func<string, T> stdoutSelector, Func<string, T> stderrSelector) =>
            spawner.Spawn(path, args, null, stdoutSelector, stderrSelector);
    }

    public static class Spawner
    {
        public static ISpawner Default => new SysSpawner();

        sealed class SysSpawner : ISpawner
        {
            public IObservable<T> Spawn<T>(string path, ProgramArguments args, string workingDirectory, Func<string, T> stdoutSelector, Func<string, T> stderrSelector) =>
                SpawnCore(path, args, workingDirectory, stdoutSelector, stderrSelector).ToObservable();
        }

        // TODO Make true observable
        static IEnumerable<T> SpawnCore<T>(string path, ProgramArguments args, string workingDirectory, Func<string, T> stdoutSelector, Func<string, T> stderrSelector)
        {
            using (var process = Process.Start(new ProcessStartInfo(path, args.ToString())
            {
                CreateNoWindow         = true,
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                WorkingDirectory       = workingDirectory,
            }))
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
