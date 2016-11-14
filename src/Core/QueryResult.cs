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
        public static IEnumerator<QueryResultItem<T>> Singleton<T>(QueryContext context, T value) =>
            Singleton(QueryResultItem.Create(context, value));

        public static IEnumerator<QueryResultItem<T>> Singleton<T>(QueryResultItem<T> item) =>
            Create(new[] { item });

        public static IEnumerator<QueryResultItem<T>> Create<T>(IEnumerable<QueryResultItem<T>> items) =>
            Create(items.GetEnumerator);

        public static IEnumerator<QueryResultItem<T>> Create<T>(Func<IEnumerator<QueryResultItem<T>>> enumeratorFactory) =>
            new Enumerator<T>(enumeratorFactory);

        public static IEnumerator<QueryResultItem<T>> Create<T>(IEnumerator<QueryResultItem<T>> items) =>
            new Enumerator<T>(items);

        public static IEnumerator<QueryResultItem<T>> Empty<T>(QueryContext context) =>
            Create(Enumerable.Empty<QueryResultItem<T>>().GetEnumerator());

        sealed class Enumerator<T> : IEnumerator<QueryResultItem<T>>
        {
            Func<IEnumerator<QueryResultItem<T>>> _enumeratorFactory;
            IEnumerator<QueryResultItem<T>> _enumerator;

            public Enumerator(Func<IEnumerator<QueryResultItem<T>>> enumeratorFactory)
            {
                _enumeratorFactory = enumeratorFactory;
            }

            public Enumerator(IEnumerator<QueryResultItem<T>> enumerator)
            {
                _enumerator = enumerator;
            }

            IEnumerator<QueryResultItem<T>> InnerEnumerator
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

            public void Dispose()
            {
                _enumeratorFactory = null;
                var enumerator = _enumerator;
                _enumerator = null;
                enumerator?.Dispose();
            }

            public bool MoveNext() => InnerEnumerator.MoveNext();
            public void Reset() => InnerEnumerator.Reset();
            public QueryResultItem<T> Current => InnerEnumerator.Current;
            object IEnumerator.Current => Current;
        }
    }

}