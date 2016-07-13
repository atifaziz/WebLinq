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

    public class Query<T>
    {
        public static Query<T> Empty = Query.Create(QueryResult.Empty<T>);

        readonly Func<QueryContext, QueryResult<T>> _func;

        internal Query(Func<QueryContext, QueryResult<T>> func)
        {
            _func = func;
        }

        public QueryResult<T> GetResult(QueryContext context) => _func(context);

        public Query<TResult> Bind<TResult>(Func<T, Query<TResult>> func)
        {
            return Query.Create(context =>
            {
                var result = GetResult(context);
                return result.HasData
                     ? func(result.Data).GetResult(result.Context)
                     : QueryResult.Empty<TResult>(context);
            });
        }

        public SeqQuery<TResult> Bind<TResult>(Func<T, SeqQuery<TResult>> func)
        {
            return SeqQuery.Create(context =>
            {
                var result = GetResult(context);
                return result.HasData
                     ? func(result.Data).GetResult(result.Context)
                     : QueryResult.Empty<IEnumerable<TResult>>(context);
            });
        }

        public Query<T> Do(Action<T> action) =>
            Select(e => { action(e); return e; });

        // LINQ support

        public Query<TResult> Select<TResult>(Func<T, TResult> selector) =>
            Bind(x => Query.Return(selector(x)));

        public Query<T> Where(Func<T, bool> predicate) =>
            Bind(x => predicate(x) ? Query.Return(x) : Empty);

        public Query<TResult> SelectMany<T2, TResult>(Func<T, Query<T2>> then,
                                                      Func<T, T2, TResult> resultSelector) =>
            Bind(x => then(x).Bind(y => Query.Return(resultSelector(x, y))));

        public SeqQuery<TResult> SelectMany<T2, TResult>(Func<T, SeqQuery<T2>> then, Func<T, T2, TResult> resultSelector) =>
            Bind(x => then(x).Bind(ys => SeqQuery.Create(context => QueryResult.Create(context, from y in ys select resultSelector(x, y)))));
    }
}