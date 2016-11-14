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
    using System.Collections.Generic;
    using System.Linq;

    // LINQ support

    static partial class Query
    {
        static ITerminalEnumerable<T> ToTerminalEnumerable<T>(this IEnumerator<T> enumerator) =>
            new TerminalEnumerable<T>(enumerator);

        static IEnumerable<QueryContext, TReturn> LiftToTerminalEnumerable<T, TReturn>(this IEnumerable<QueryContext, T> query, Func<IEnumerable<StateItemPair<QueryContext, T>>, IEnumerable<StateItemPair<QueryContext, TReturn>>> func) =>
            query.Bind(xs => Return(func(xs.ToTerminalEnumerable())));

        public static IEnumerable<QueryContext, TResult> Select<T, TResult>(this IEnumerable<QueryContext, T> query, Func<T, TResult> selector) =>
           query.LiftToTerminalEnumerable(xs => from x in xs
                                      select x.WithValue(selector(x.Item)));

        public static IEnumerable<QueryContext, T> Where<T>(this IEnumerable<QueryContext, T> query, Func<T, bool> predicate) =>
            query.LiftToTerminalEnumerable(xs => from x in xs
                                       where predicate(x.Item)
                                       select x);

        public static IEnumerable<QueryContext, TResult> SelectMany<T1, T2, TResult>(this IEnumerable<QueryContext, T1> query, Func<T1, IEnumerable<T2>> f, Func<T1, T2, TResult> g) =>
            query.Bind(xs => Create(context => QueryResult.Create(SelectManyIterator(context, xs, f, g))));

        static IEnumerator<StateItemPair<QueryContext, TResult>> SelectManyIterator<T1, T2, TResult>(QueryContext context, IEnumerator<StateItemPair<QueryContext, T1>> xs, Func<T1, IEnumerable<T2>> f, Func<T1, T2, TResult> g)
        {
            var q =
                from x in xs.ToTerminalEnumerable()
                from result in f(x.Item)
                select QueryResultItem.Create(x.State, g(x.Item, result));

            foreach (var e in q)
                yield return e;
        }

        public static IEnumerable<QueryContext, TResult> SelectMany<T1, T2, TResult>(this IEnumerable<QueryContext, T1> query, Func<T1, IEnumerable<QueryContext, T2>> f, Func<T1, T2, TResult> g) =>
            query.Bind(xs => Create(context => QueryResult.Create(SelectManyIterator(context, xs, f, g))));

        static IEnumerator<StateItemPair<QueryContext, TResult>> SelectManyIterator<T1, T2, TResult>(QueryContext context, IEnumerator<StateItemPair<QueryContext, T1>> xs, Func<T1, IEnumerable<QueryContext, T2>> f, Func<T1, T2, TResult> g)
        {
            var q =
                from x in xs.ToTerminalEnumerable()
                from result in f(x.Item).GetEnumerator(x.State).ToTerminalEnumerable()
                select QueryResultItem.Create(result.State, g(x.Item, result.Item));

            foreach (var e in q)
                yield return e;
        }

        public static IEnumerable<QueryContext, TResult> Aggregate<T, TState, TResult>(this IEnumerable<QueryContext, T> query, TState seed,
            Func<TState, T, TState> accumulator,
            Func<TState, TResult> resultSelector)
        {
            if (accumulator == null) throw new ArgumentNullException(nameof(accumulator));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            return from context in GetContext()
                   select query.GetEnumerator(context)
                               .ToTerminalEnumerable()
                               .Select(e => e.Item)
                               .Aggregate(seed, accumulator, resultSelector);
        }

        public static IEnumerable<QueryContext, T> SkipWhile<T>(this IEnumerable<QueryContext, T> query, Func<T, bool> predicate) =>
            query.LiftToTerminalEnumerable(xs => xs.SkipWhile(x => predicate(x.Item)));

        public static IEnumerable<QueryContext, T> TakeWhile<T>(this IEnumerable<QueryContext, T> query, Func<T, bool> predicate) =>
            query.LiftToTerminalEnumerable(xs => xs.TakeWhile(x => predicate(x.Item)));

        public static IEnumerable<QueryContext, T> Skip<T>(this IEnumerable<QueryContext, T> query, int count) =>
            query.LiftToTerminalEnumerable(e => e.Skip(count));

        public static IEnumerable<QueryContext, T> Take<T>(this IEnumerable<QueryContext, T> query, int count) =>
            query.LiftToTerminalEnumerable(e => e.Take(count));

        public static IEnumerable<QueryContext, T> Distinct<T>(this IEnumerable<QueryContext, T> query) =>
            query.LiftToTerminalEnumerable(Enumerable.Distinct);

        public static IEnumerable<QueryContext, T> Distinct<T>(this IEnumerable<QueryContext, T> query, IEqualityComparer<T> comparer) =>
            query.LiftToTerminalEnumerable(xs => xs.Distinct(comparer.ContraMap<T, StateItemPair<QueryContext, T>> (x => x.Item)));

        public static IEnumerable<QueryContext, T> Concat<T>(this IEnumerable<QueryContext, T> first, IEnumerable<QueryContext, T> second) =>
            first.Bind(xs => second.Bind(ys => Return(xs.ToTerminalEnumerable().Concat(ys.ToTerminalEnumerable()))));

        public static StateItemPair<QueryContext, T> Single<T>(this IEnumerable<QueryContext, T> query, QueryContext context)
        {
            using (var e = query.GetEnumerator(context))
            {
                if (!e.MoveNext())
                    throw new InvalidOperationException();
                var item = e.Current;
                if (e.MoveNext())
                    throw new InvalidOperationException();
                return item;    
            }
        }
    }
}
