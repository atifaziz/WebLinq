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
        public static IEnumerator<StateItemPair<TState, T>> Singleton<TState, T>(TState context, T value) =>
            Singleton(StateItemPair.Create(context, value));

        public static IEnumerator<StateItemPair<TState, T>> Singleton<TState, T>(StateItemPair<TState, T> item) =>
            Create(new[] { item });

        public static IEnumerator<StateItemPair<TState, T>> Create<TState, T>(IEnumerable<StateItemPair<TState, T>> items) =>
            Create(items.GetEnumerator);

        public static IEnumerator<StateItemPair<TState, T>> Create<TState, T>(Func<IEnumerator<StateItemPair<TState, T>>> enumeratorFactory) =>
            new QueryResultEnumerator<TState, T>(enumeratorFactory);

        public static IEnumerator<StateItemPair<TState, T>> Create<TState, T>(IEnumerator<StateItemPair<TState, T>> items) =>
            new QueryResultEnumerator<TState, T>(items);

        public static IEnumerator<StateItemPair<TState, T>> Empty<TState, T>(TState context) =>
            Create(Enumerable.Empty<StateItemPair<TState, T>>().GetEnumerator());

        sealed class QueryResultEnumerator<TState, T> : IEnumerator<StateItemPair<TState, T>>
        {
            Func<IEnumerator<StateItemPair<TState, T>>> _enumeratorFactory;
            IEnumerator<StateItemPair<TState, T>> _enumerator;

            public QueryResultEnumerator(Func<IEnumerator<StateItemPair<TState, T>>> enumeratorFactory)
            {
                _enumeratorFactory = enumeratorFactory;
            }

            public QueryResultEnumerator(IEnumerator<StateItemPair<TState, T>> enumerator)
            {
                _enumerator = enumerator;
            }

            IEnumerator<StateItemPair<TState, T>> InnerEnumerator
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
            public StateItemPair<TState, T> Current => InnerEnumerator.Current;
            object IEnumerator.Current => Current;
        }
    }

}