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

namespace WebLinq.Experimental
{
    using System;
    using System.Globalization;

    // ReSharper disable InconsistentNaming
    // ReSharper disable PartialTypeWithSinglePart

    public sealed class RW<E, T>
    {
        readonly Func<E, T> _func;

        public RW(Func<E, T> func) { _func = func; }
        public T Run(E e) => _func(e);
    }

    public static class RW
    {
        public static RW<E, T> Return<E, T>(Func<E, T> func) =>
            new RW<E, T>(func);

        public static RW<E, U> Bind<E, T, U>(this RW<E, T> io, Func<T, RW<E, U>> f) =>
            Return((E e) => f(io.Run(e)).Run(e));

        public static RW<E, U> Select<E, T, U>(this RW<E, T> io, Func<T, U> selector) =>
            io.Bind(x => Return((E e) => selector(x)));

        public static RW<E, V> SelectMany<E, T, U, V>(this RW<E, T> first, Func<T, RW<E, U>> secondSelector, Func<T, U, V> resultSelector) =>
            first.Bind(x => secondSelector(x).Bind(y => Return((E _) => resultSelector(x, y))));

        public static RW<E, int> Int32<E>(this RW<E, string> r) =>
            from v in r select int.Parse(v, CultureInfo.InvariantCulture);
    }
}