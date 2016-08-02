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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Mannex.Collections.Generic;

    public static class SysQuery
    {
        public static Query<string> Spawn(string path, string args) =>
            Spawn(path, args, output => output, null);

        public static Query<KeyValuePair<T, string>> Spawn<T>(string path, string args, T stdoutKey, T stderrKey) =>
            Spawn(path, args, stdout => stdoutKey.AsKeyTo(stdout),
                              stderr => stderrKey.AsKeyTo(stderr));

        public static Query<T> Spawn<T>(string path, string args, Func<string, T> stdoutSelector, Func<string, T> stderrSelector) =>
            Query.Create(context => QueryResult.Create(from e in context.Eval((ISpawnService s) => s.Spawn(path, args, stdoutSelector, stderrSelector))
                                                       select QueryResultItem.Create(context, e)));
    }
}
