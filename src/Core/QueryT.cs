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

    public partial class Query<T>
    {
        public static Query<T> Empty = Query.Create(QueryResult.Empty<T>);

        readonly Func<QueryContext, QueryResult<T>> _func;

        internal Query(Func<QueryContext, QueryResult<T>> func)
        {
            _func = func;
        }

        public QueryResult<T> GetResult(QueryContext context) => _func(context);

        public Query<TResult> Bind<TResult>(Func<QueryResult<T>, Query<TResult>> func)
        {
            return Query.Create(context =>
            {
                var result = GetResult(context);
                var q = func(result);
                return q.GetResult(context);
            });
        }

        public Query<T> Do(Action<T> action) =>
            Select(e => { action(e); return e; });

        public Query<T> Wait() =>
            Bind(xs => Query.Return(xs.ToArray()));

        // LINQ support

        Query<TReturn> LiftEnumerable<TReturn>(Func<IEnumerable<QueryResultItem<T>>, IEnumerable<QueryResultItem<TReturn>>> func) =>
            Bind(xs => Query.Return(func(xs)));

        public Query<TResult> Select<TResult>(Func<T, TResult> selector) =>
            LiftEnumerable(xs => from x in xs
                                 select x.WithValue(selector(x.Value)));

        public Query<T> Where(Func<T, bool> predicate) =>
            LiftEnumerable(xs => from x in xs
                                 where predicate(x.Value)
                                 select x);

        public Query<TResult> SelectMany<T2, TResult>(Func<T, Query<T2>> f, Func<T, T2, TResult> g) =>
            Bind(xs => Query.Create(context => QueryResult.Create(SelectManyIterator(context, xs, f, g))));

        static IEnumerable<QueryResultItem<TResult>> SelectManyIterator<T2, TResult>(QueryContext context, QueryResult<T> xs, Func<T, Query<T2>> f, Func<T, T2, TResult> g) =>
            from x in xs
            from result in f(x.Value).GetResult(x.Context)
            select QueryResultItem.Create(result.Context, g(x, result.Value));

        public Query<TResult> Aggregate<TState, TResult>(TState seed,
            Func<TState, T, TState> accumulator,
            Func<TState, TResult> resultSelector)
        {
            if (accumulator == null) throw new ArgumentNullException(nameof(accumulator));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            return from context in Query.GetContext()
                   select GetResult(context).Select(e => e.Value)
                                            .Aggregate(seed, accumulator, resultSelector);
        }

        public Query<T> SkipWhile(Func<T, bool> predicate) =>
            LiftEnumerable(xs => xs.SkipWhile(x => predicate(x.Value)));

        public Query<T> TakeWhile(Func<T, bool> predicate) =>
            LiftEnumerable(xs => xs.TakeWhile(x => predicate(x.Value)));

        public Query<T> Skip(int count) =>
            LiftEnumerable(e => e.Skip(count));

        public Query<T> Take(int count) =>
            LiftEnumerable(e => e.Skip(count));

        public Query<T> Concat(Query<T> query) =>
            Bind(xs => query.Bind(ys => Query.Return(xs.Concat(ys))));
    }
}
