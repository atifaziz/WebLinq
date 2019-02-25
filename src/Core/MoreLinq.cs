#region License and Terms
// MoreLINQ - Extensions to LINQ to Objects
// Copyright (c) 2008 Jonathan Skeet. All rights reserved.
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
#endregion

namespace MoreLinq
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    static partial class MoreEnumerable
    {
        /// <summary>
        /// Returns the set of elements in the first sequence which aren't
        /// in the second sequence, according to a given key selector.
        /// </summary>
        /// <remarks>
        /// This is a set operation; if multiple elements in <paramref name="first"/> have
        /// equal keys, only the first such element is returned.
        /// This operator uses deferred execution and streams the results, although
        /// a set of keys from <paramref name="second"/> is immediately selected and retained.
        /// </remarks>
        /// <typeparam name="TSource">The type of the elements in the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="first">The sequence of potentially included elements.</param>
        /// <param name="second">The sequence of elements whose keys may prevent elements in
        /// <paramref name="first"/> from being returned.</param>
        /// <param name="keySelector">The mapping from source element to key.</param>
        /// <returns>A sequence of elements from <paramref name="first"/> whose key was not also a key for
        /// any element in <paramref name="second"/>.</returns>

        public static IEnumerable<TSource> ExceptBy<TSource, TKey>(this IEnumerable<TSource> first,
            IEnumerable<TSource> second,
            Func<TSource, TKey> keySelector)
        {
            return ExceptBy(first, second, keySelector, null);
        }

        /// <summary>
        /// Returns the set of elements in the first sequence which aren't
        /// in the second sequence, according to a given key selector.
        /// </summary>
        /// <remarks>
        /// This is a set operation; if multiple elements in <paramref name="first"/> have
        /// equal keys, only the first such element is returned.
        /// This operator uses deferred execution and streams the results, although
        /// a set of keys from <paramref name="second"/> is immediately selected and retained.
        /// </remarks>
        /// <typeparam name="TSource">The type of the elements in the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="first">The sequence of potentially included elements.</param>
        /// <param name="second">The sequence of elements whose keys may prevent elements in
        /// <paramref name="first"/> from being returned.</param>
        /// <param name="keySelector">The mapping from source element to key.</param>
        /// <param name="keyComparer">The equality comparer to use to determine whether or not keys are equal.
        /// If null, the default equality comparer for <c>TSource</c> is used.</param>
        /// <returns>A sequence of elements from <paramref name="first"/> whose key was not also a key for
        /// any element in <paramref name="second"/>.</returns>

        public static IEnumerable<TSource> ExceptBy<TSource, TKey>(this IEnumerable<TSource> first,
            IEnumerable<TSource> second,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> keyComparer)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

            return _(); IEnumerable<TSource>_()
            {
                // TODO Use ToHashSet
                var keys = new HashSet<TKey>(second.Select(keySelector), keyComparer);
                foreach (var element in first)
                {
                    var key = keySelector(element);
                    if (keys.Contains(key))
                        continue;
                    yield return element;
                    keys.Add(key);
                }
            }
        }
    }
}

namespace MoreLinq
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    static partial class MoreEnumerable
    {
        /// <summary>
        /// Performs a left outer join on two homogeneous sequences.
        /// Additional arguments specify key selection functions and result
        /// projection functions.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">
        /// The type of the key returned by the key selector function.</typeparam>
        /// <typeparam name="TResult">
        /// The type of the result elements.</typeparam>
        /// <param name="first">
        /// The first sequence of the join operation.</param>
        /// <param name="second">
        /// The second sequence of the join operation.</param>
        /// <param name="keySelector">
        /// Function that projects the key given an element of one of the
        /// sequences to join.</param>
        /// <param name="firstSelector">
        /// Function that projects the result given just an element from
        /// <paramref name="first"/> where there is no corresponding element
        /// in <paramref name="second"/>.</param>
        /// <param name="bothSelector">
        /// Function that projects the result given an element from
        /// <paramref name="first"/> and an element from <paramref name="second"/>
        /// that match on a common key.</param>
        /// <returns>A sequence containing results projected from a left
        /// outer join of the two input sequences.</returns>

        public static IEnumerable<TResult> LeftJoin<TSource, TKey, TResult>(
            this IEnumerable<TSource> first,
            IEnumerable<TSource> second,
            Func<TSource, TKey> keySelector,
            Func<TSource, TResult> firstSelector,
            Func<TSource, TSource, TResult> bothSelector)
        {
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            return first.LeftJoin(second, keySelector,
                                  firstSelector, bothSelector,
                                  null);
        }

        /// <summary>
        /// Performs a left outer join on two homogeneous sequences.
        /// Additional arguments specify key selection functions, result
        /// projection functions and a key comparer.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">
        /// The type of the key returned by the key selector function.</typeparam>
        /// <typeparam name="TResult">
        /// The type of the result elements.</typeparam>
        /// <param name="first">
        /// The first sequence of the join operation.</param>
        /// <param name="second">
        /// The second sequence of the join operation.</param>
        /// <param name="keySelector">
        /// Function that projects the key given an element of one of the
        /// sequences to join.</param>
        /// <param name="firstSelector">
        /// Function that projects the result given just an element from
        /// <paramref name="first"/> where there is no corresponding element
        /// in <paramref name="second"/>.</param>
        /// <param name="bothSelector">
        /// Function that projects the result given an element from
        /// <paramref name="first"/> and an element from <paramref name="second"/>
        /// that match on a common key.</param>
        /// <param name="comparer">
        /// The <see cref="IEqualityComparer{T}"/> instance used to compare
        /// keys for equality.</param>
        /// <returns>A sequence containing results projected from a left
        /// outer join of the two input sequences.</returns>

        public static IEnumerable<TResult> LeftJoin<TSource, TKey, TResult>(
            this IEnumerable<TSource> first,
            IEnumerable<TSource> second,
            Func<TSource, TKey> keySelector,
            Func<TSource, TResult> firstSelector,
            Func<TSource, TSource, TResult> bothSelector,
            IEqualityComparer<TKey> comparer)
        {
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            return first.LeftJoin(second,
                                  keySelector, keySelector,
                                  firstSelector, bothSelector,
                                  comparer);
        }

        /// <summary>
        /// Performs a left outer join on two heterogeneous sequences.
        /// Additional arguments specify key selection functions and result
        /// projection functions.
        /// </summary>
        /// <typeparam name="TFirst">
        /// The type of elements in the first sequence.</typeparam>
        /// <typeparam name="TSecond">
        /// The type of elements in the second sequence.</typeparam>
        /// <typeparam name="TKey">
        /// The type of the key returned by the key selector functions.</typeparam>
        /// <typeparam name="TResult">
        /// The type of the result elements.</typeparam>
        /// <param name="first">
        /// The first sequence of the join operation.</param>
        /// <param name="second">
        /// The second sequence of the join operation.</param>
        /// <param name="firstKeySelector">
        /// Function that projects the key given an element from <paramref name="first"/>.</param>
        /// <param name="secondKeySelector">
        /// Function that projects the key given an element from <paramref name="second"/>.</param>
        /// <param name="firstSelector">
        /// Function that projects the result given just an element from
        /// <paramref name="first"/> where there is no corresponding element
        /// in <paramref name="second"/>.</param>
        /// <param name="bothSelector">
        /// Function that projects the result given an element from
        /// <paramref name="first"/> and an element from <paramref name="second"/>
        /// that match on a common key.</param>
        /// <returns>A sequence containing results projected from a left
        /// outer join of the two input sequences.</returns>

        public static IEnumerable<TResult> LeftJoin<TFirst, TSecond, TKey, TResult>(
            this IEnumerable<TFirst> first,
            IEnumerable<TSecond> second,
            Func<TFirst, TKey> firstKeySelector,
            Func<TSecond, TKey> secondKeySelector,
            Func<TFirst, TResult> firstSelector,
            Func<TFirst, TSecond, TResult> bothSelector) =>
            first.LeftJoin(second,
                           firstKeySelector, secondKeySelector,
                           firstSelector, bothSelector,
                           null);

        /// <summary>
        /// Performs a left outer join on two heterogeneous sequences.
        /// Additional arguments specify key selection functions, result
        /// projection functions and a key comparer.
        /// </summary>
        /// <typeparam name="TFirst">
        /// The type of elements in the first sequence.</typeparam>
        /// <typeparam name="TSecond">
        /// The type of elements in the second sequence.</typeparam>
        /// <typeparam name="TKey">
        /// The type of the key returned by the key selector functions.</typeparam>
        /// <typeparam name="TResult">
        /// The type of the result elements.</typeparam>
        /// <param name="first">
        /// The first sequence of the join operation.</param>
        /// <param name="second">
        /// The second sequence of the join operation.</param>
        /// <param name="firstKeySelector">
        /// Function that projects the key given an element from <paramref name="first"/>.</param>
        /// <param name="secondKeySelector">
        /// Function that projects the key given an element from <paramref name="second"/>.</param>
        /// <param name="firstSelector">
        /// Function that projects the result given just an element from
        /// <paramref name="first"/> where there is no corresponding element
        /// in <paramref name="second"/>.</param>
        /// <param name="bothSelector">
        /// Function that projects the result given an element from
        /// <paramref name="first"/> and an element from <paramref name="second"/>
        /// that match on a common key.</param>
        /// <param name="comparer">
        /// The <see cref="IEqualityComparer{T}"/> instance used to compare
        /// keys for equality.</param>
        /// <returns>A sequence containing results projected from a left
        /// outer join of the two input sequences.</returns>

        public static IEnumerable<TResult> LeftJoin<TFirst, TSecond, TKey, TResult>(
            this IEnumerable<TFirst> first,
            IEnumerable<TSecond> second,
            Func<TFirst, TKey> firstKeySelector,
            Func<TSecond, TKey> secondKeySelector,
            Func<TFirst, TResult> firstSelector,
            Func<TFirst, TSecond, TResult> bothSelector,
            IEqualityComparer<TKey> comparer)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));
            if (firstKeySelector == null) throw new ArgumentNullException(nameof(firstKeySelector));
            if (secondKeySelector == null) throw new ArgumentNullException(nameof(secondKeySelector));
            if (firstSelector == null) throw new ArgumentNullException(nameof(firstSelector));
            if (bothSelector == null) throw new ArgumentNullException(nameof(bothSelector));

            KeyValuePair<TK, TV> Pair<TK, TV>(TK k, TV v) => new KeyValuePair<TK, TV>(k, v);

            return // TODO replace KeyValuePair<,> with (,) for clarity
                from j in first.GroupJoin(second, firstKeySelector, secondKeySelector,
                                          (f, ss) => Pair(f, from s in ss select Pair(true, s)),
                                          comparer)
                from s in j.Value.DefaultIfEmpty()
                select s.Key ? bothSelector(j.Key, s.Value) : firstSelector(j.Key);
        }
    }
}

namespace MoreLinq
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    // Inspiration & credit: http://stackoverflow.com/a/13503860/6682
    static partial class MoreEnumerable
    {
        /// <summary>
        /// Performs a Full Group Join between the <paramref name="first"/> and <paramref name="second"/> sequences.
        /// </summary>
        /// <remarks>
        /// This operator uses deferred execution and streams the results.
        /// The results are yielded in the order of the elements found in the first sequence
        /// followed by those found only in the second. In addition, the callback responsible
        /// for projecting the results is supplied with sequences which preserve their source order.
        /// </remarks>
        /// <typeparam name="TFirst">The type of the elements in the first input sequence</typeparam>
        /// <typeparam name="TSecond">The type of the elements in the second input sequence</typeparam>
        /// <typeparam name="TKey">The type of the key to use to join</typeparam>
        /// <param name="first">First sequence</param>
        /// <param name="second">Second sequence</param>
        /// <param name="firstKeySelector">The mapping from first sequence to key</param>
        /// <param name="secondKeySelector">The mapping from second sequence to key</param>
        /// <returns>A sequence of elements joined from <paramref name="first"/> and <paramref name="second"/>.
        /// </returns>

        public static IEnumerable<(TKey Key, IEnumerable<TFirst> First, IEnumerable<TSecond> Second)> FullGroupJoin<TFirst, TSecond, TKey>(this IEnumerable<TFirst> first,
            IEnumerable<TSecond> second,
            Func<TFirst, TKey> firstKeySelector,
            Func<TSecond, TKey> secondKeySelector)
        {
            return FullGroupJoin(first, second, firstKeySelector, secondKeySelector, ValueTuple.Create, EqualityComparer<TKey>.Default);
        }

        /// <summary>
        /// Performs a Full Group Join between the <paramref name="first"/> and <paramref name="second"/> sequences.
        /// </summary>
        /// <remarks>
        /// This operator uses deferred execution and streams the results.
        /// The results are yielded in the order of the elements found in the first sequence
        /// followed by those found only in the second. In addition, the callback responsible
        /// for projecting the results is supplied with sequences which preserve their source order.
        /// </remarks>
        /// <typeparam name="TFirst">The type of the elements in the first input sequence</typeparam>
        /// <typeparam name="TSecond">The type of the elements in the second input sequence</typeparam>
        /// <typeparam name="TKey">The type of the key to use to join</typeparam>
        /// <param name="first">First sequence</param>
        /// <param name="second">Second sequence</param>
        /// <param name="firstKeySelector">The mapping from first sequence to key</param>
        /// <param name="secondKeySelector">The mapping from second sequence to key</param>
        /// <param name="comparer">The equality comparer to use to determine whether or not keys are equal.
        /// If null, the default equality comparer for <c>TKey</c> is used.</param>
        /// <returns>A sequence of elements joined from <paramref name="first"/> and <paramref name="second"/>.
        /// </returns>

        public static IEnumerable<(TKey Key, IEnumerable<TFirst> First, IEnumerable<TSecond> Second)> FullGroupJoin<TFirst, TSecond, TKey>(this IEnumerable<TFirst> first,
            IEnumerable<TSecond> second,
            Func<TFirst, TKey> firstKeySelector,
            Func<TSecond, TKey> secondKeySelector,
            IEqualityComparer<TKey> comparer)
        {
            return FullGroupJoin(first, second, firstKeySelector, secondKeySelector, ValueTuple.Create, comparer);
        }

        /// <summary>
        /// Performs a full group-join between two sequences.
        /// </summary>
        /// <remarks>
        /// This operator uses deferred execution and streams the results.
        /// The results are yielded in the order of the elements found in the first sequence
        /// followed by those found only in the second. In addition, the callback responsible
        /// for projecting the results is supplied with sequences which preserve their source order.
        /// </remarks>
        /// <typeparam name="TFirst">The type of the elements in the first input sequence</typeparam>
        /// <typeparam name="TSecond">The type of the elements in the second input sequence</typeparam>
        /// <typeparam name="TKey">The type of the key to use to join</typeparam>
        /// <typeparam name="TResult">The type of the elements of the resulting sequence</typeparam>
        /// <param name="first">First sequence</param>
        /// <param name="second">Second sequence</param>
        /// <param name="firstKeySelector">The mapping from first sequence to key</param>
        /// <param name="secondKeySelector">The mapping from second sequence to key</param>
        /// <param name="resultSelector">Function to apply to each pair of elements plus the key</param>
        /// <returns>A sequence of elements joined from <paramref name="first"/> and <paramref name="second"/>.
        /// </returns>

        public static IEnumerable<TResult> FullGroupJoin<TFirst, TSecond, TKey, TResult>(this IEnumerable<TFirst> first,
            IEnumerable<TSecond> second,
            Func<TFirst, TKey> firstKeySelector,
            Func<TSecond, TKey> secondKeySelector,
            Func<TKey, IEnumerable<TFirst>, IEnumerable<TSecond>, TResult> resultSelector)
        {
            return FullGroupJoin(first, second, firstKeySelector, secondKeySelector, resultSelector, EqualityComparer<TKey>.Default);
        }

        /// <summary>
        /// Performs a full group-join between two sequences.
        /// </summary>
        /// <remarks>
        /// This operator uses deferred execution and streams the results.
        /// The results are yielded in the order of the elements found in the first sequence
        /// followed by those found only in the second. In addition, the callback responsible
        /// for projecting the results is supplied with sequences which preserve their source order.
        /// </remarks>
        /// <typeparam name="TFirst">The type of the elements in the first input sequence</typeparam>
        /// <typeparam name="TSecond">The type of the elements in the second input sequence</typeparam>
        /// <typeparam name="TKey">The type of the key to use to join</typeparam>
        /// <typeparam name="TResult">The type of the elements of the resulting sequence</typeparam>
        /// <param name="first">First sequence</param>
        /// <param name="second">Second sequence</param>
        /// <param name="firstKeySelector">The mapping from first sequence to key</param>
        /// <param name="secondKeySelector">The mapping from second sequence to key</param>
        /// <param name="resultSelector">Function to apply to each pair of elements plus the key</param>
        /// <param name="comparer">The equality comparer to use to determine whether or not keys are equal.
        /// If null, the default equality comparer for <c>TKey</c> is used.</param>
        /// <returns>A sequence of elements joined from <paramref name="first"/> and <paramref name="second"/>.
        /// </returns>

        public static IEnumerable<TResult> FullGroupJoin<TFirst, TSecond, TKey, TResult>(this IEnumerable<TFirst> first,
            IEnumerable<TSecond> second,
            Func<TFirst, TKey> firstKeySelector,
            Func<TSecond, TKey> secondKeySelector,
            Func<TKey, IEnumerable<TFirst>, IEnumerable<TSecond>, TResult> resultSelector,
            IEqualityComparer<TKey> comparer)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));
            if (firstKeySelector == null) throw new ArgumentNullException(nameof(firstKeySelector));
            if (secondKeySelector == null) throw new ArgumentNullException(nameof(secondKeySelector));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            return _(); IEnumerable<TResult> _()
            {
                comparer = comparer ?? EqualityComparer<TKey>.Default;

                var alookup = Lookup<TKey,TFirst>.CreateForJoin(first, firstKeySelector, comparer);
                var blookup = Lookup<TKey, TSecond>.CreateForJoin(second, secondKeySelector, comparer);

                foreach (var a in alookup) {
                    yield return resultSelector(a.Key, a, blookup[a.Key]);
                }

                foreach (var b in blookup) {
                    if (alookup.Contains(b.Key))
                        continue;
                    // We can skip the lookup because we are iterating over keys not found in the first sequence
                    yield return resultSelector(b.Key, Enumerable.Empty<TFirst>(), b);
                }
            }
        }
    }
}

#region License and Terms
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
// The MIT License (MIT)
//
// Copyright(c) Microsoft Corporation
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
#endregion

namespace MoreLinq
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// A <see cref="ILookup{TKey, TElement}"/> implementation that preserves insertion order
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the <see cref="Lookup{TKey, TElement}"/></typeparam>
    /// <typeparam name="TElement">The type of the elements in the <see cref="IEnumerable{T}"/> sequences that make up the values in the <see cref="Lookup{TKey, TElement}"/></typeparam>
    /// <remarks>
    /// This implementation preserves insertion order of keys and elements within each <see cref="IEnumerable{T}"/>
    /// Copied over from CoreFX on 2015-10-27
    /// https://github.com/dotnet/corefx/blob/6f1c2a86fb8fa1bdaee7c6e70a684d27842d804c/src/System.Linq/src/System/Linq/Enumerable.cs#L3230-L3403
    /// Modified to remove internal interfaces
    /// </remarks>
    internal class Lookup<TKey, TElement> : IEnumerable<IGrouping<TKey, TElement>>, ILookup<TKey, TElement>
    {
        private IEqualityComparer<TKey> _comparer;
        private Grouping<TKey, TElement>[] _groupings;
        private Grouping<TKey, TElement> _lastGrouping;
        private int _count;

        internal static Lookup<TKey, TElement> Create<TSource>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            if (elementSelector == null) throw new ArgumentNullException(nameof(elementSelector));
            Lookup<TKey, TElement> lookup = new Lookup<TKey, TElement>(comparer);
            foreach (TSource item in source) {
                lookup.GetGrouping(keySelector(item), true).Add(elementSelector(item));
            }
            return lookup;
        }

        internal static Lookup<TKey, TElement> CreateForJoin(IEnumerable<TElement> source, Func<TElement, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            Lookup<TKey, TElement> lookup = new Lookup<TKey, TElement>(comparer);
            foreach (TElement item in source) {
                TKey key = keySelector(item);
                if (key != null) lookup.GetGrouping(key, true).Add(item);
            }
            return lookup;
        }

        private Lookup(IEqualityComparer<TKey> comparer)
        {
            if (comparer == null) comparer = EqualityComparer<TKey>.Default;
            _comparer = comparer;
            _groupings = new Grouping<TKey, TElement>[7];
        }

        public int Count
        {
            get { return _count; }
        }

        public IEnumerable<TElement> this[TKey key]
        {
            get
            {
                Grouping<TKey, TElement> grouping = GetGrouping(key, false);
                if (grouping != null) return grouping;
                return Enumerable.Empty<TElement>();
            }
        }

        public bool Contains(TKey key)
        {
            return _count > 0 && GetGrouping(key, false) != null;
        }

        public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator()
        {
            Grouping<TKey, TElement> g = _lastGrouping;
            if (g != null) {
                do {
                    g = g.next;
                    yield return g;
                } while (g != _lastGrouping);
            }
        }

        public IEnumerable<TResult> ApplyResultSelector<TResult>(Func<TKey, IEnumerable<TElement>, TResult> resultSelector)
        {
            Grouping<TKey, TElement> g = _lastGrouping;
            if (g != null) {
                do {
                    g = g.next;
                    if (g.count != g.elements.Length) { Array.Resize<TElement>(ref g.elements, g.count); }
                    yield return resultSelector(g.key, g.elements);
                } while (g != _lastGrouping);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal int InternalGetHashCode(TKey key)
        {
            // Handle comparer implementations that throw when passed null
            return (key == null) ? 0 : _comparer.GetHashCode(key) & 0x7FFFFFFF;
        }

        internal Grouping<TKey, TElement> GetGrouping(TKey key, bool create)
        {
            int hashCode = InternalGetHashCode(key);
            for (Grouping<TKey, TElement> g = _groupings[hashCode % _groupings.Length]; g != null; g = g.hashNext)
                if (g.hashCode == hashCode && _comparer.Equals(g.key, key)) return g;
            if (create) {
                if (_count == _groupings.Length) Resize();
                int index = hashCode % _groupings.Length;
                Grouping<TKey, TElement> g = new Grouping<TKey, TElement>();
                g.key = key;
                g.hashCode = hashCode;
                g.elements = new TElement[1];
                g.hashNext = _groupings[index];
                _groupings[index] = g;
                if (_lastGrouping == null) {
                    g.next = g;
                }
                else {
                    g.next = _lastGrouping.next;
                    _lastGrouping.next = g;
                }
                _lastGrouping = g;
                _count++;
                return g;
            }
            return null;
        }

        private void Resize()
        {
            int newSize = checked(_count * 2 + 1);
            Grouping<TKey, TElement>[] newGroupings = new Grouping<TKey, TElement>[newSize];
            Grouping<TKey, TElement> g = _lastGrouping;
            do {
                g = g.next;
                int index = g.hashCode % newSize;
                g.hashNext = newGroupings[index];
                newGroupings[index] = g;
            } while (g != _lastGrouping);
            _groupings = newGroupings;
        }
    }

    internal class Grouping<TKey, TElement> : IGrouping<TKey, TElement>, IList<TElement>
    {
        internal TKey key;
        internal int hashCode;
        internal TElement[] elements;
        internal int count;
        internal Grouping<TKey, TElement> hashNext;
        internal Grouping<TKey, TElement> next;

        internal Grouping()
        {
        }

        internal void Add(TElement element)
        {
            if (elements.Length == count) Array.Resize(ref elements, checked(count * 2));
            elements[count] = element;
            count++;
        }

        public IEnumerator<TElement> GetEnumerator()
        {
            for (int i = 0; i < count; i++) yield return elements[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        // DDB195907: implement IGrouping<>.Key implicitly
        // so that WPF binding works on this property.
        public TKey Key
        {
            get { return key; }
        }

        int ICollection<TElement>.Count
        {
            get { return count; }
        }

        bool ICollection<TElement>.IsReadOnly
        {
            get { return true; }
        }

        void ICollection<TElement>.Add(TElement item)
        {
            throw new NotSupportedException("Lookup is immutable");
        }

        void ICollection<TElement>.Clear()
        {
            throw new NotSupportedException("Lookup is immutable");
        }

        bool ICollection<TElement>.Contains(TElement item)
        {
            return Array.IndexOf(elements, item, 0, count) >= 0;
        }

        void ICollection<TElement>.CopyTo(TElement[] array, int arrayIndex)
        {
            Array.Copy(elements, 0, array, arrayIndex, count);
        }

        bool ICollection<TElement>.Remove(TElement item)
        {
            throw new NotSupportedException("Lookup is immutable");
        }

        int IList<TElement>.IndexOf(TElement item)
        {
            return Array.IndexOf(elements, item, 0, count);
        }

        void IList<TElement>.Insert(int index, TElement item)
        {
            throw new NotSupportedException("Lookup is immutable");
        }

        void IList<TElement>.RemoveAt(int index)
        {
            throw new NotSupportedException("Lookup is immutable");
        }

        TElement IList<TElement>.this[int index]
        {
            get
            {
                if (index < 0 || index >= count) throw new ArgumentOutOfRangeException(nameof(index));
                return elements[index];
            }
            set
            {
                throw new NotSupportedException("Lookup is immutable");
            }
        }
    }
}
