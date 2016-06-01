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

        public static IEnumerable<T> ToEnumerable<T>(this Query<IEnumerable<T>> query, QueryContext context)
        {
            var result = query.GetResult(context);
            return result.DataOrDefault() ?? Enumerable.Empty<T>();
        }

        public static IEnumerable<T> ToEnumerable<T>(this SeqQuery<T> query, Func<QueryContext> contextFactory)
        {
            var result = query.GetResult(contextFactory());
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var e in result.DataOrDefault() ?? Enumerable.Empty<T>())
                yield return e;
        }
    }
}
