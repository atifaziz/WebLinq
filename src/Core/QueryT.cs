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

    public static class StateItemPair
    {
        public static StateItemPair<TState, T> Create<TState, T>(TState state, T item) =>
            new StateItemPair<TState, T>(item, state);
    }

    public struct StateItemPair<TState, T> : IEquatable<StateItemPair<TState, T>>
    {
        public readonly T Item;
        public readonly TState State;

        public StateItemPair(T item, TState state)
        {
            Item = item;
            State = state;
        }

        public bool Equals(StateItemPair<TState, T> other) =>
            EqualityComparer<T>.Default.Equals(Item, other.Item)
            && EqualityComparer<TState>.Default.Equals(State, other.State);

        public override bool Equals(object obj) =>
            obj is StateItemPair<TState, T> && Equals((StateItemPair<TState, T>) obj);

        public override int GetHashCode() =>
            unchecked(EqualityComparer<T>.Default.GetHashCode(Item) * 397 ^ EqualityComparer<TState>.Default.GetHashCode(State));

        public static bool operator ==(StateItemPair<TState, T> left, StateItemPair<TState, T> right) =>
            left.Equals(right);

        public static bool operator !=(StateItemPair<TState, T> left, StateItemPair<TState, T> right) =>
            !left.Equals(right);
    }

    public interface IEnumerable<TState, T>
    {
        IEnumerator<StateItemPair<TState, T>> GetEnumerator(TState context);
    }

    public interface IQuery<T> : IEnumerable<QueryContext, T>
    {
        QueryResult<T> GetResult(QueryContext context);
    }

    partial class Query<T> : IQuery<T>
    {
        public static IQuery<T> Empty = Query.Create(QueryResult.Empty<T>);

        readonly Func<QueryContext, QueryResult<T>> _func;

        internal Query(Func<QueryContext, QueryResult<T>> func)
        {
            _func = func;
        }

        public QueryResult<T> GetResult(QueryContext context) => _func(context);
        public IEnumerator<StateItemPair<QueryContext, T>> GetEnumerator(QueryContext context)
        {
            using (var e = GetResult(context))
                while (e.MoveNext())
                    yield return StateItemPair.Create(e.Current.Context, e.Current.Value);
        }
    }
}
