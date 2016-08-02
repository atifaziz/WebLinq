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
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    public static class QueryResult
    {
        public static QueryResult<T> Singleton<T>(QueryContext context, T data) =>
            Singleton(QueryResultItem.Create(context, data));
        public static QueryResult<T> Singleton<T>(QueryResultItem<T> item) =>
            new QueryResult<T>(new [] { item });
        public static QueryResult<T> Create<T>(IEnumerable<QueryResultItem<T>> items) =>
            new QueryResult<T>(items);
        public static QueryResult<T> Empty<T>(QueryContext context) =>
            new QueryResult<T>(Enumerable.Empty<QueryResultItem<T>>());
    }

    public sealed class QueryResult<T> : IEnumerable<QueryResultItem<T>>
    {
        readonly IEnumerable<QueryResultItem<T>> _data;

        public QueryResult(IEnumerable<QueryResultItem<T>> data)
        {
            _data = data;
        }

        public IEnumerator<QueryResultItem<T>> GetEnumerator() => _data.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public static explicit operator T(QueryResult<T> result) => result.Single().Data;
    }

    public static class QueryResultItem
    {
        public static QueryResultItem<T> Create<T>(QueryContext context, T data) =>
            new QueryResultItem<T>(context, data);
    }

    [DebuggerDisplay("{Data}")]
    public sealed class QueryResultItem<T>
    {
        public T Data { get; }
        public QueryContext Context { get; }

        public QueryResultItem(QueryContext context, T data)
        {
            Context = context;
            Data = data;
        }

        public QueryResultItem<TResult> WithData<TResult>(TResult data) =>
            QueryResultItem.Create(Context, data);

        public QueryResultItem<T> WithContext(QueryContext context) =>
            QueryResultItem.Create(context, Data);

        public static implicit operator T(QueryResultItem<T> result) => result.Data;
        public override string ToString() => $"{Data}";
    }
}