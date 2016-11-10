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

    public static class Comparer
    {
        public static EqualityComparer<T, TResult> ContraMap<T, TResult>(this IEqualityComparer<T> comparer, Func<TResult, T> contraMapper)
            => new EqualityComparer<T, TResult>(comparer, contraMapper);

        public sealed class EqualityComparer<T, TResult> : IEqualityComparer<TResult>
        {
            readonly Func<TResult, T> _contraMapper;
            readonly IEqualityComparer<T> _comparer;

            public EqualityComparer(IEqualityComparer<T> comparer, Func<TResult, T> contraMapper)
            {
                if (contraMapper == null) throw new ArgumentNullException(nameof(contraMapper));
                if (comparer == null) throw new ArgumentNullException(nameof(comparer));
                _contraMapper = contraMapper;
                _comparer = comparer;
            }

            public bool Equals(TResult x, TResult y) => _comparer.Equals(_contraMapper(x), _contraMapper(y));
            public int GetHashCode(TResult obj) => _comparer.GetHashCode(_contraMapper(obj));
        }
    }
}
