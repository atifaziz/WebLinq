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

    public static class Query
    {
        public static Query<int> Sequence(int first, int last) =>
            Sequence(first, last, 1);

        public static Query<int> Sequence(int first, int last, int step)
        {
            if (step <= 0)
                throw new ArgumentException(null, nameof(step));
            if (last < first)
                step = -step;
            return MoreEnumerable.Generate(first, i => i + step)
                                 .TakeWhile(i => step < 0 ? i >= last : i <= last)
                                 .ToQuery();
        }

        public static Query<T> ToQuery<T>(this IEnumerable<T> items) =>
            Create(context => QueryResult.Create(from item in items select QueryResultItem.Create(context, item)));

        public static Query<QueryContext> GetContext() =>
            Create(context => QueryResult.Singleton(context, context));

        public static Query<QueryContext> SetContext(QueryContext newContext) =>
            Create(context => QueryResult.Singleton(newContext, context));

        public static Query<T> Create<T>(Func<QueryContext, QueryResult<T>> func) =>
            new Query<T>(func);

        public static Query<T> Create<T>(Func<QueryContext, IEnumerable<QueryResultItem<T>>> func) =>
            new Query<T>(context => QueryResult.Create(func(context)));

        public static Query<T> Singleton<T>(T item) =>
            Return(new[] { item });

        public static Query<T> Return<T>(IEnumerable<T> items) =>
            items.ToQuery();

        public static Query<T> Return<T>(IEnumerable<QueryResultItem<T>> items) =>
            Create(context => QueryResult.Create(items));

        //public static Query<T> Spread<T>(this Query<IEnumerable<T>> query) =>
        //    Create(query.GetResult);
        //
        public static IEnumerable<T> ToEnumerable<T>(this Query<T> query, Func<QueryContext> contextFactory) =>
            from e in query.GetResult(contextFactory())
            select e.Value;

        public static Query<T> FindService<T>() where T : class =>
            from context in GetContext()
            select (IServiceProvider) context into sp
            select (T)sp.GetService(typeof(T));

        public static Query<T> GetItem<T>(string key) =>
            from x in TryGetItem(key, (bool found, T value) => new { Found = found, Value = value })
            where x.Found
            select x.Value;

        public static Query<TResult> TryGetItem<T, TResult>(string key, Func<bool, T, TResult> resultSelector) =>
            from context in GetContext()
            select context.Items.TryGetValue(key, (some, value) => some ? resultSelector(true, (T) value)
                                                                        : resultSelector(false, default(T)));

        public static Query<TResult> SetItem<T, TResult>(string key, T value, Func<bool, T, TResult> resultSelector) =>
            from ov in TryGetItem(key, resultSelector)
            from context in GetContext()
            from _ in SetContext(context.WithItems(context.Items.Set(key, value)))
            select ov;

        public static Query<T> GetService<T>() where T : class =>
            from context in GetContext()
            select context.GetService<T>();

        public static Query<T> SetService<T>(T service) where T : class =>
            from current in FindService<T>()
            from context in GetContext()
            from _ in SetContext(context.WithServiceProvider(context.LinkService(typeof(T), service)))
            select current;
    }
}
