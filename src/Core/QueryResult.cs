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
    using System.Diagnostics;

    public static class QueryResult
    {
        public static QueryResult<T> Create<T>(QueryContext context, T data) =>
            new QueryResult<T>(context, data);
        public static QueryResult<T> Empty<T>(QueryContext context) =>
            new QueryResult<T>(context);
    }

    [DebuggerDisplay("{Data}")]
    public sealed class QueryResult<T>
    {
        readonly T _data;

        public QueryContext Context { get; }
        public bool IsEmpty => !HasData;
        public bool HasData { get; }

        public T Data
        {
            get
            {
                if (IsEmpty) throw new InvalidOperationException();
                return _data;
            }
        }

        public QueryResult(QueryContext context) :
            this(context, false, default(T)) {}

        public QueryResult(QueryContext context, T data) :
            this(context, true, data) {}

        QueryResult(QueryContext context, bool hasData, T data)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            Context = context;
            HasData = hasData;
            _data = data;
        }

        public T DataOrDefault() => DataOrDefault(default(T));
        public T DataOrDefault(T defaultValue) => HasData ? Data : defaultValue;

        public static implicit operator T(QueryResult<T> result) => result.Data;

        public override string ToString() => HasData ? Data.ToString() : string.Empty;
    }
}