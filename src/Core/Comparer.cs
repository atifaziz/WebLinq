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

        sealed public class EqualityComparer<T, TResult> : IEqualityComparer<TResult>
        {
            Func<TResult, T> contraMapper;
            IEqualityComparer<T> comparer;

            public EqualityComparer(IEqualityComparer<T> comparer, Func<TResult, T> contraMapper)
            {
                if (contraMapper == null) throw new ArgumentNullException(nameof(contraMapper));
                if (comparer == null) throw new ArgumentNullException(nameof(comparer));
                this.contraMapper = contraMapper;
                this.comparer = comparer;
            }

            public bool Equals(TResult x, TResult y) => comparer.Equals(contraMapper(x), contraMapper(y));
            public int GetHashCode(TResult obj) => comparer.GetHashCode(contraMapper(obj));
        }
    }
}