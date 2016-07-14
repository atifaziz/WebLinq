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

    public static class Query
    {
        public static Query<T> Create<T>(Func<QueryContext, QueryResult<T>> func) =>
            new Query<T>(func);

        public static Query<T> Return<T>(T value) =>
            Create(context => QueryResult.Create(context, value));

        public static SeqQuery<T> Spread<T>(this Query<IEnumerable<T>> query) =>
            SeqQuery.Create(query.GetResult);

        [Obsolete("Use the overload taking a context factory instead.")]
        public static IEnumerable<T> ToEnumerable<T>(this Query<IEnumerable<T>> query, QueryContext context)
        {
            var result = query.GetResult(context);
            return result.DataOrDefault() ?? Enumerable.Empty<T>();
        }

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
