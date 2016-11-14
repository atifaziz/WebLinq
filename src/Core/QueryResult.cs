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
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public static class QueryResult
    {
        public static QueryResult<T> Singleton<T>(QueryContext context, T value) =>
            Singleton(QueryResultItem.Create(context, value));

        public static QueryResult<T> Singleton<T>(QueryResultItem<T> item) =>
            Create(new[] { item }.AsEnumerable().GetEnumerator());

        [Obsolete]
        public static QueryResult<T> Create<T>(IEnumerable<QueryResultItem<T>> items) =>
            new QueryResult3<T>(items.GetEnumerator);

        public static QueryResult<T> Create<T>(IEnumerator<QueryResultItem<T>> items) =>
            new QueryResult2<T>(items);

        public static QueryResult<T> Empty<T>(QueryContext context) =>
            Create(Enumerable.Empty<QueryResultItem<T>>().GetEnumerator());

        sealed class QueryResult3<T> : QueryResult<T>
        {
            Func<IEnumerator<QueryResultItem<T>>> _enumeratorFactory;
            IEnumerator<QueryResultItem<T>> _enumerator;

            public QueryResult3(Func<IEnumerator<QueryResultItem<T>>> enumeratorFactory)
            {
                _enumeratorFactory = enumeratorFactory;
            }

            IEnumerator<QueryResultItem<T>> Enumerator
            {
                get
                {
                    if (_enumeratorFactory != null)
                    {
                        _enumerator = _enumeratorFactory();
                        _enumeratorFactory = null;
                    }
                    else if (_enumerator == null)
                    {
                        throw new ObjectDisposedException(GetType().Name);
                    }

                    return _enumerator;
                }
            }

            public override void Dispose()
            {
                _enumeratorFactory = null;
                var enumerator = _enumerator;
                _enumerator = null;
                enumerator?.Dispose();
            }

            public override bool MoveNext() => Enumerator.MoveNext();
            public override void Reset() => Enumerator.Reset();
            public override QueryResultItem<T> Current => Enumerator.Current;
        }

        sealed class QueryResult2<T> : QueryResult<T>
        {
            IEnumerator<QueryResultItem<T>> _enumerator;

            public QueryResult2(IEnumerator<QueryResultItem<T>> enumerator)
            {
                _enumerator = enumerator;
            }

            IEnumerator<QueryResultItem<T>> Enumerator
            {
                get
                {
                    if (_enumerator == null) throw new ObjectDisposedException(GetType().Name);
                    return _enumerator;
                }
            }

            public override void Dispose()
            {
                var enumerator = _enumerator;
                _enumerator = null;
                enumerator?.Dispose();
            }

            public override bool MoveNext() => Enumerator.MoveNext();
            public override void Reset() => Enumerator.Reset();
            public override QueryResultItem<T> Current => Enumerator.Current;

            //public static explicit operator T(QueryResult<T> result) => result.Single().Value;
        }
    }

    public abstract class QueryResult<T> : IEnumerator<QueryResultItem<T>>
    {
        public abstract void Dispose();
        public abstract bool MoveNext();
        public abstract void Reset();
        public abstract QueryResultItem<T> Current { get; }
        object IEnumerator.Current => Current;
    }
}