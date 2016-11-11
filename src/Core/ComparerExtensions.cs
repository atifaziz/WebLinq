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

    static class ComparerExtensions
    {
        public static IEqualityComparer<TResult> ContraMap<T, TResult>(this IEqualityComparer<T> comparer, Func<TResult, T> mapper) =>
            new ContraMappingEqualityComparer<T, TResult>(comparer, mapper);

        sealed class ContraMappingEqualityComparer<T, TResult> : IEqualityComparer<TResult>
        {
            readonly IEqualityComparer<T> _comparer;
            readonly Func<TResult, T> _mapper;

            public ContraMappingEqualityComparer(IEqualityComparer<T> comparer, Func<TResult, T> mapper)
            {
                if (mapper == null) throw new ArgumentNullException(nameof(mapper));
                if (comparer == null) throw new ArgumentNullException(nameof(comparer));
                _mapper = mapper;
                _comparer = comparer;
            }

            public bool Equals(TResult x, TResult y) =>
                _comparer.Equals(_mapper(x), _mapper(y));

            public int GetHashCode(TResult obj) =>
                _comparer.GetHashCode(_mapper(obj));
        }
    }
}
