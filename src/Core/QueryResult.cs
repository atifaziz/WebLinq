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
        public static IEnumerator<StateItemPair<QueryContext, T>> Singleton<T>(QueryContext context, T value) =>
            Singleton(StateItemPair.Create(context, value));

        public static IEnumerator<StateItemPair<QueryContext, T>> Singleton<T>(StateItemPair<QueryContext, T> item) =>
            Create(new[] { item });

        public static IEnumerator<StateItemPair<QueryContext, T>> Create<T>(IEnumerable<StateItemPair<QueryContext, T>> items) =>
            Create(items.GetEnumerator);

        public static IEnumerator<StateItemPair<QueryContext, T>> Create<T>(Func<IEnumerator<StateItemPair<QueryContext, T>>> enumeratorFactory) =>
            new QueryResultEnumerator<T>(enumeratorFactory);

        public static IEnumerator<StateItemPair<QueryContext, T>> Create<T>(IEnumerator<StateItemPair<QueryContext, T>> items) =>
            new QueryResultEnumerator<T>(items);

        public static IEnumerator<StateItemPair<QueryContext, T>> Empty<T>(QueryContext context) =>
            Create(Enumerable.Empty<StateItemPair<QueryContext, T>>().GetEnumerator());

        sealed class QueryResultEnumerator<T> : IEnumerator<StateItemPair<QueryContext, T>>
        {
            Func<IEnumerator<StateItemPair<QueryContext, T>>> _enumeratorFactory;
            IEnumerator<StateItemPair<QueryContext, T>> _enumerator;

            public QueryResultEnumerator(Func<IEnumerator<StateItemPair<QueryContext, T>>> enumeratorFactory)
            {
                _enumeratorFactory = enumeratorFactory;
            }

            public QueryResultEnumerator(IEnumerator<StateItemPair<QueryContext, T>> enumerator)
            {
                _enumerator = enumerator;
            }

            IEnumerator<StateItemPair<QueryContext, T>> InnerEnumerator
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
            public StateItemPair<QueryContext, T> Current => InnerEnumerator.Current;
            object IEnumerator.Current => Current;
        }
    }

}