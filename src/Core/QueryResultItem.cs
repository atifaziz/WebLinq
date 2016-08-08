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
    using System.Diagnostics;

    public static class QueryResultItem
    {
        public static QueryResultItem<T> Create<T>(QueryContext context, T items) =>
            new QueryResultItem<T>(context, items);
    }

    [DebuggerDisplay("{Value}")]
    public sealed class QueryResultItem<T>
    {
        public T Value { get; }
        public QueryContext Context { get; }

        public QueryResultItem(QueryContext context, T value)
        {
            Context = context;
            Value = value;
        }

        public QueryResultItem<TResult> WithValue<TResult>(TResult value) =>
            QueryResultItem.Create(Context, value);

        public QueryResultItem<T> WithContext(QueryContext context) =>
            QueryResultItem.Create(context, Value);

        public static implicit operator T(QueryResultItem<T> result) => result.Value;
        public override string ToString() => $"{Value}";
    }
}