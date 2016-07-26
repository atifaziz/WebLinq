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
    using MoreLinq;

    public static class SeqQuery2
    {
        public static SeqQuery2<int> Sequence(int first, int last) =>
            Sequence(first, last, 1);

        public static SeqQuery2<int> Sequence(int first, int last, int step)
        {
            if (step <= 0)
                throw new ArgumentException(null, nameof(step));
            if (last < first)
                step = -step;
            return MoreEnumerable.Generate(first, i => i + step)
                                 .TakeWhile(i => step < 0 ? i >= last : i <= last)
                                 .ToQuery2();
        }

        public static SeqQuery2<T> Create<T>(Func<QueryContext, IEnumerable<QueryResult<T>>> func) =>
            new SeqQuery2<T>(func);

        public static SeqQuery2<T> Return<T>(IEnumerable<T> values) =>
            Create(context => from v in values select QueryResult.Create(context, v));

        public static SeqQuery2<T> Return<T>(IEnumerable<QueryResult<T>> values) =>
            // ReSharper disable once PossibleMultipleEnumeration
            Create(context => values);

        public static SeqQuery2<T> ToQuery2<T>(this IEnumerable<T> value) =>
            Return(value);

        public static IEnumerable<T> ToEnumerable<T>(this SeqQuery2<T> query, Func<QueryContext> contextFactory)
        {
            var result = query.GetResult(contextFactory());
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var e in result)
                if (e.HasData)
                    yield return e.Data;
        }
    }

    public class SeqQuery2<T>
    {
        public static SeqQuery2<T> Empty = SeqQuery2.Create(delegate { return Enumerable.Empty<QueryResult<T>>(); });

        readonly Func<QueryContext, IEnumerable<QueryResult<T>>> _func;

        internal SeqQuery2(Func<QueryContext, IEnumerable<QueryResult<T>>> func)
        {
            _func = func;
        }

        public IEnumerable<QueryResult<T>> GetResult(QueryContext context) => _func(context);

        public SeqQuery2<TResult> Bind<TResult>(Func<IEnumerable<QueryResult<T>>, SeqQuery2<TResult>> func) =>
            SeqQuery2.Create(context =>
            {
                var result = GetResult(context);
                var q = func(result);
                return q.GetResult(context);
            });

        // LINQ support

        public SeqQuery2<TResult> Select<TResult>(Func<T, TResult> selector) =>
            Bind(xs => SeqQuery2.Return(from x in xs select selector(x)));

        public SeqQuery2<T> Where(Func<T, bool> predicate) =>
            Bind(xs => SeqQuery2.Return(from x in xs
                                        where x.HasData && predicate(x.Data)
                                        select x.Data));

        public SeqQuery2<TResult> SelectMany<T2, TResult>(Func<T, Query<T2>> f, Func<T, T2, TResult> g) =>
            Bind(xs => SeqQuery2.Create(s => SelectManyIterator(s, xs, f, g)));

        static IEnumerable<QueryResult<TResult>> SelectManyIterator<T2, TResult>(QueryContext context, IEnumerable<QueryResult<T>> xs, Func<T, Query<T2>> f, Func<T, T2, TResult> g) =>
            from x in xs
            where x.HasData
            let result = f(x).GetResult(x.Context)
            where result.HasData
            select QueryResult.Create(result.Context, g(x, result.Data));

        public SeqQuery2<TResult> SelectMany<T2, TResult>(Func<T, SeqQuery2<T2>> f, Func<T, T2, TResult> g) =>
            Bind(xs => SeqQuery2.Create(s => SelectManyIterator(s, xs, f, g)));

        static IEnumerable<QueryResult<TResult>> SelectManyIterator<T2, TResult>(QueryContext context, IEnumerable<QueryResult<T>> xs, Func<T, SeqQuery2<T2>> f, Func<T, T2, TResult> g) =>
            from x in xs
            where x.HasData
            from y in f(x).GetResult(x.Context)
            select QueryResult.Create(y.Context, g(x, y));
    }
}