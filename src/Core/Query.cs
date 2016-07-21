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
        public static SeqQuery<int> Sequence(int first, int last) =>
            Sequence(first, last, 1);

        public static SeqQuery<int> Sequence(int first, int last, int step)
        {
            if (step <= 0)
                throw new ArgumentException(null, nameof(step));
            if (last < first)
                step = -step;
            return MoreEnumerable.Generate(first, i => i + step)
                                 .TakeWhile(i => step < 0 ? i >= last : i <= last)
                                 .ToQuery();
        }

        public static Query<QueryContext> Context() =>
            Create(context => QueryResult.Create(context, context));

        public static Query<QueryContext> Context(QueryContext newContext) =>
            Create(context => QueryResult.Create(newContext, context));

        public static Query<T> Create<T>(Func<QueryContext, QueryResult<T>> func) =>
            new Query<T>(func);

        public static Query<T> Return<T>(T value) =>
            Create(context => QueryResult.Create(context, value));

        public static SeqQuery<T> Spread<T>(this Query<IEnumerable<T>> query) =>
            SeqQuery.Create(query.GetResult);

        public static IEnumerable<T> ToEnumerable<T>(this Query<IEnumerable<T>> query, Func<QueryContext> contextFactory) =>
            query.Spread().ToEnumerable(contextFactory);

        public static Query<T> FindService<T>() where T : class =>
            Create(context =>
            {
                IServiceProvider sp = context;
                return QueryResult.Create(context, (T) sp.GetService(typeof(T)));
            });

        public static Query<T> GetService<T>() where T : class =>
            Create(context => QueryResult.Create(context, context.GetService<T>()));

        public static Query<T> SetService<T>(T service) where T : class =>
            FindService<T>().Bind(current => Create(context =>
                QueryResult.Create(new QueryContext(context.LinkService(typeof(T), service)), current)));
    }
}
