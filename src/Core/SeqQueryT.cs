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

    public class SeqQuery<T>
    {
        public static SeqQuery<T> Empty = SeqQuery.Create(QueryResult.Empty<IEnumerable<T>>);

        readonly Func<QueryContext, QueryResult<IEnumerable<T>>> _func;

        internal SeqQuery(Func<QueryContext, QueryResult<IEnumerable<T>>> func)
        {
            _func = func;
        }

        public QueryResult<IEnumerable<T>> GetResult(QueryContext context) => _func(context);

        public SeqQuery<TResult> Bind<TResult>(Func<IEnumerable<T>, SeqQuery<TResult>> func)
        {
            return SeqQuery.Create(context =>
                                   {
                                       var result = GetResult(context);
                                       return result.HasData
                                           ? func(result.Data).GetResult(result.Context)
                                           : QueryResult.Empty<IEnumerable<TResult>>(context);
                                   });
        }

        public Query<TResult> Aggregate<TState, TResult>(TState seed,
            Func<TState, T, TState> accumulator,
            Func<TState, TResult> resultSelector)
        {
            if (accumulator == null) throw new ArgumentNullException(nameof(accumulator));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            return Query.Create(context =>
                                {
                                    var result = GetResult(context);
                                    return result.HasData
                                        ? QueryResult.Create(context, result.Data.Aggregate(seed, accumulator, resultSelector))
                                        : QueryResult.Empty<TResult>(context);
                                });
        }

        // LINQ support

        public SeqQuery<TResult> Select<TResult>(Func<T, TResult> selector) =>
            Bind(xs => SeqQuery.Return(from x in xs select selector(x)));

        public SeqQuery<T> Where(Func<T, bool> predicate) =>
            Bind(xs => SeqQuery.Return(from x in xs where predicate(x) select x));

        public SeqQuery<TResult> SelectMany<T2, TResult>(Func<T, Query<T2>> f, Func<T, T2, TResult> g) =>
            Bind(xs => SeqQuery.Create(s => QueryResult.Create(s, SelectManyIterator(s, xs, f, g))));

        static IEnumerable<TResult> SelectManyIterator<T2, TResult>(QueryContext context, IEnumerable<T> xs, Func<T, Query<T2>> f, Func<T, T2, TResult> g)
        {
            foreach (var x in xs)
            {
                var y = f(x).GetResult(context);
                if (y.HasData)
                    yield return g(x, y.Data);
            }
        }

        public SeqQuery<TResult> SelectMany<T2, TResult>(Func<T, SeqQuery<T2>> f, Func<T, T2, TResult> g) =>
            Bind(xs => SeqQuery.Create(s => QueryResult.Create(s, SelectManyIterator(s, xs, f, g))));

        static IEnumerable<TResult> SelectManyIterator<T2, TResult>(QueryContext context, IEnumerable<T> xs, Func<T, SeqQuery<T2>> f, Func<T, T2, TResult> g)
        {
            foreach (var x in xs)
            {
                var ys = f(x).GetResult(context);
                if (ys.HasData)
                {
                    foreach (var y in ys.Data)
                        yield return g(x, y);
                }
            }
        }

        public SeqQuery<T> Concat(SeqQuery<T> other) =>
            Bind(xs => other.Bind(ys => SeqQuery.Return(xs.Concat(ys))));

        public SeqQuery<T> OrderBy<TKey>(Func<T, TKey> keySelector) =>
            Bind(xs => SeqQuery.Return(xs.OrderBy(keySelector)));

        public SeqQuery<T> Distinct() => Distinct(null);

        public SeqQuery<T> Distinct(IEqualityComparer<T> comparer) =>
            Bind(xs => SeqQuery.Return(xs.Distinct(comparer)));
    }
}