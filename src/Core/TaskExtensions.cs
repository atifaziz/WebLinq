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

namespace WebLinq
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

    static class TaskExtensions
    {
        public static ConfiguredTaskAwaitable DontContinueOnCapturedContext(this Task task)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));
            return task.ConfigureAwait(continueOnCapturedContext: false);
        }

        public static ConfiguredTaskAwaitable<T> DontContinueOnCapturedContext<T>(this Task<T> task)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));
            return task.ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}
