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
    using System.Reactive.Linq;

    public delegate T Factory<out T>();

    public static class Factory
    {
        public static Factory<T> Return<T>(T value) => () => value;
        public static Factory<T> Create<T>(Factory<T> factory) => factory;

        public static Factory<TResult> Bind<T, TResult>(this Factory<T> factory, Func<T, Factory<TResult>> func) =>
            func(factory());

        public static Factory<TResult> Select<T, TResult>(this Factory<T> factory, Func<T, TResult> func) =>
            factory.Bind(e => Create(() => func(e)));

        public static Factory<TResult> SelectMany<TFirst, TSecond, TResult>(this Factory<TFirst> first,
            Func<TFirst, Factory<TSecond>> secondSelector,
            Func<TFirst, TSecond, TResult> resultSelector) =>
            first.Bind(fst => secondSelector(fst).Bind(snd => Create(() => resultSelector(fst, snd))));

        public static IObservable<Factory<TResult>> SelectMany<TFirst, TSecond, TResult>(this Factory<TFirst> first,
            Func<TFirst, IObservable<TSecond>> secondSelector,
            Func<TFirst, TSecond, TResult> resultSelector) =>
            from fst in Observable.Defer(() => Observable.Return(first()))
            from e in secondSelector(fst)
            select Create(() => resultSelector(fst, e));
    }
}