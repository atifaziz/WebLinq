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

    /// <summary>
    /// Indicates an <see cref="IEnumerable{T}"/> that can be iterated once
    /// only and throws <see cref="InvalidOperationException"/> on all
    /// subsequent attempts to iterate.
    /// </summary>

    interface ITerminalEnumerable<out T> : IEnumerable<T> { }

    sealed class TerminalEnumerable<T> : ITerminalEnumerable<T>
    {
        IEnumerator<T> _inner;

        public TerminalEnumerable(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<T> GetEnumerator()
        {
            if (_inner == null)
                throw new InvalidOperationException();
            var inner = _inner;
            _inner = null;
            return GetEnumerator(inner);
        }

        static IEnumerator<T> GetEnumerator(IEnumerator<T> inner)
        {
            while (inner.MoveNext())
                yield return inner.Current;
        }
    }
}