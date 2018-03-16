#region Copyright (c) 2017 Atif Aziz. All rights reserved.
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
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive;

    public delegate T Reader<in TEnvironment, out T>(TEnvironment environment);

    public static class Reader
    {
        public static Reader<TEnvironment, T> Return<TEnvironment, T>(T value) => _ => value;

        public static Reader<TEnvironment, TResult> Bind<TEnvironment, T, TResult>(this Reader<TEnvironment, T> reader, Func<T, Reader<TEnvironment, TResult>> selector) =>
            env => selector(reader(env))(env);

        public static Reader<TEnvironment, TResult> Select<TEnvironment, T, TResult>(this Reader<TEnvironment, T> reader, Func<T, TResult> selector) =>
            env => selector(reader(env));

        public static Reader<TEnvironment, TResult> SelectMany<TEnvironment, TFirst, TSecond, TResult>(
            this Reader<TEnvironment, TFirst> reader,
            Func<TFirst, Reader<TEnvironment, TSecond>> secondSelector,
            Func<TFirst, TSecond, TResult> resultSelector) =>
            env =>
            {
                var t = reader(env);
                return resultSelector(t, secondSelector(t)(env));
            };

        public static Reader<TEnvironment, IEnumerable<TResult>> For<TEnvironment, T, TResult>(IEnumerable<T> source,
            Func<T, Reader<TEnvironment, TResult>> f) =>
            coll => source.Select(e => f(e)).Select(e => e(coll)).ToList();

        public static Reader<TEnvironment, T> Do<TEnvironment, T>(this Reader<TEnvironment, T> reader, Action<TEnvironment> action) =>
            reader.Bind<TEnvironment, T, T>(x => env => { action(env); return x; });

        public static Reader<TEnvironment, Unit> Do<TEnvironment>(Action<TEnvironment> action) =>
            env => { action(env); return new Unit(); };
    }
}
