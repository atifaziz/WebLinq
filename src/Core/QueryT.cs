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

    public interface IQuery<TState, T>
    {
        StateItemPair<TState, T> GetResult(TState state);
    }

    public sealed class Query<TState, T> : IQuery<TState, T>
    {
        readonly Func<TState, StateItemPair<TState, T>> _func;

        public Query(Func<TState, StateItemPair<TState, T>> func)
        {
            _func = func;
        }

        public StateItemPair<TState, T> GetResult(TState state) =>
            _func(state);
    }

    public static class StateItemPair
    {
        public static StateItemPair<TState, T> Create<TState, T>(TState state, T item) =>
            new StateItemPair<TState, T>(state, item);
    }

    public struct StateItemPair<TState, T> : IEquatable<StateItemPair<TState, T>>
    {
        public TState State { get; }
        public T Item { get; }

        public StateItemPair(TState state, T item)
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

        public StateItemPair<TState, T2> WithValue<T2>(T2 item) => StateItemPair.Create(State, item);
        public StateItemPair<TState, T> WithContext(TState state) => StateItemPair.Create(state, Item);
    }

    public interface IEnumerable<TState, T>
    {
        IEnumerator<StateItemPair<TState, T>> GetEnumerator(TState state);
    }

    public interface IServicableEnumerable<TState, T>
        : IEnumerable<TState, T>
        where TState : IServiceProvider {}

    public static class SeqQuery
    {
        public static IEnumerable<TState, T> Create<TState, T>(Func<TState, IEnumerator<StateItemPair<TState, T>>> func) =>
            new SeqQuery<TState, T>(func);
    }

    partial class SeqQuery<TState, T> : IEnumerable<TState, T>
    {
        public static IEnumerable<TState, T> Empty = SeqQuery.Create<TState, T>(QueryResult.Empty<TState, T>);

        readonly Func<TState, IEnumerator<StateItemPair<TState, T>>> _func;

        internal SeqQuery(Func<TState, IEnumerator<StateItemPair<TState, T>>> func)
        {
            _func = func;
        }

        public IEnumerator<StateItemPair<TState, T>> GetResult(TState state) => _func(state);
        public IEnumerator<StateItemPair<TState, T>> GetEnumerator(TState state)
        {
            using (var e = GetResult(state))
                while (e.MoveNext())
                    yield return StateItemPair.Create(e.Current.State, e.Current.Item);
        }
    }
}
