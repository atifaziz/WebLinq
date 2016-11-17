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

    public static partial class Query
    {
        public static IQuery<TState, T> Create<TState, T>(Func<TState, StateItemPair<TState, T>> func) =>
            new Query<TState, T>(func);

        public static IQuery<TState, TResult> Bind<TState, T, TResult>(
                this IQuery<TState, T> f, Func<T, IQuery<TState, TResult>> g) =>
            Create((TState state) =>
            {
                var x = f.GetResult(state);
                return g(x.Item).GetResult(x.State);
            });

        public static IEnumerable<TState, TResult> Bind<TState, T, TResult>(this IEnumerable<TState, T> query, Func<IEnumerator<StateItemPair<TState, T>>, IEnumerable<TState, TResult>> func) =>
            SeqQuery.Create((TState state) =>
            {
                var result = query.GetEnumerator(state);
                var q = func(result);
                return q.GetEnumerator(state);
            });

        public static IEnumerable<QueryContext, T> Do<T>(this IEnumerable<QueryContext, T> query, Action<T> action) =>
            query.Select(e => { action(e); return e; });

        public static IEnumerable<QueryContext, T> Wait<T>(this IEnumerable<QueryContext, T> query) =>
            query.Bind(xs =>
            {
                var list = new List<T>();
                while (xs.MoveNext())
                    list.Add(xs.Current.Item);
                return Return(list);
            });

       public static IEnumerable<QueryContext, int> Sequence(int first, int last) =>
            Sequence(first, last, 1);

        public static IEnumerable<QueryContext, int> Sequence(int first, int last, int step)
        {
            if (step <= 0)
                throw new ArgumentException(null, nameof(step));
            if (last < first)
                step = -step;
            return MoreEnumerable.Generate(first, i => i + step)
                                 .TakeWhile(i => step < 0 ? i >= last : i <= last)
                                 .ToQuery();
        }

        public static IEnumerable<QueryContext, T> ToQuery<T>(this IEnumerable<T> items) => SeqQuery.Create((QueryContext context) => QueryResult.Create(items.GetEnumerator(context)));

        static IEnumerator<StateItemPair<QueryContext, T>> GetEnumerator<T>(this IEnumerable<T> items, QueryContext context)
        {
            var q =
                from item in items
                select StateItemPair.Create(context, item);
            foreach (var e in q)
                yield return e;
        }

        public static IEnumerable<QueryContext, QueryContext> GetContext() => SeqQuery.Create((QueryContext context) => QueryResult.Singleton(context, context));

        public static IEnumerable<QueryContext, QueryContext> SetContext(QueryContext newContext) =>
            SetContext(_ => newContext);

        public static IEnumerable<QueryContext, QueryContext> SetContext(Func<QueryContext, QueryContext> contextor) => SeqQuery.Create((QueryContext context) => QueryResult.Singleton(contextor(context), context));

        public static IEnumerable<QueryContext, T> Singleton<T>(T item) => Array(item);

        public static IEnumerable<QueryContext, T> Array<T>(params T[] items) => items.ToQuery();

        public static IEnumerable<QueryContext, T> Return<T>(IEnumerable<T> items) => items.ToQuery();

        static IEnumerable<QueryContext, T> Return<T>(IEnumerable<StateItemPair<QueryContext, T>> items) => SeqQuery.Create((QueryContext context) => QueryResult.Create(items));

        public static IEnumerable<T> ToEnumerable<T>(this IEnumerable<QueryContext, T> query, Func<QueryContext> contextFactory)
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            using (var e = query.GetEnumerator(contextFactory()))
                while (e.MoveNext())
                    yield return e.Current.Item;
        }

        public static IEnumerable<QueryContext, T> FindService<T>() where T : class =>
            from context in GetContext()
            select (IServiceProvider) context into sp
            select (T)sp.GetService(typeof(T));

        public static IEnumerable<QueryContext, T> GetItem<T>(string key) =>
            from x in TryGetItem(key, (bool found, T value) => new { Found = found, Value = value })
            where x.Found
            select x.Value;

        public static IEnumerable<QueryContext, TResult> TryGetItem<T, TResult>(string key, Func<bool, T, TResult> resultSelector) =>
            from context in GetContext()
            select context.Items.TryGetValue(key, (some, value) => some ? resultSelector(true, (T) value)
                                                                        : resultSelector(false, default(T)));

        public static IEnumerable<QueryContext, Unit> SetItem<T>(string key, T value) =>
            SetItem(key, value, delegate { return new Unit(); });

        public static IEnumerable<QueryContext, TResult> SetItem<T, TResult>(string key, T value, Func<bool, T, TResult> resultSelector) =>
            from ov in TryGetItem(key, resultSelector)
            from _ in SetContext(context => context.WithItems(context.Items.Set(key, value))).Ignore()
            select ov;

        public static IEnumerable<QueryContext, T> GetService<T>() where T : class =>
            from context in GetContext()
            select context.GetService<T>();

        public static IEnumerable<QueryContext, T> SetService<T>(T service) where T : class =>
            from current in FindService<T>()
            from _ in SetContext(context => context.WithServiceProvider(context.CacheServiceQueries().LinkService(typeof(T), service))).Ignore()
            select current;

        public static IEnumerable<QueryContext, Unit> Ignore<T>(this IEnumerable<QueryContext, T> query) =>
            from _ in query select new Unit();

        public static IEnumerable<QueryContext, T> Generate<T>(T seed, Func<T, IEnumerable<QueryContext, T>> generator) => SeqQuery.Create((QueryContext context) => QueryResult.Create(GenerateCore(context, seed, generator)));

        static IEnumerator<StateItemPair<QueryContext, T>> GenerateCore<T>(QueryContext context, T seed, Func<T, IEnumerable<QueryContext, T>> generator)
        {
            yield return QueryResultItem.Create(context, seed);
            var current = seed;
            while (true)
            {
                using (var e = generator(current).GetEnumerator(context))
                {
                    while (e.MoveNext())
                    {
                        var next = e.Current;
                        yield return next;
                        current = next.Item;
                        context = next.State;
                    }
                }
            }
        }
    }
}
