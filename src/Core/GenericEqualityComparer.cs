using System;
using System.Collections.Generic;

namespace WebLinq
{
    public class GenericEqualityComparer<T, U> : IEqualityComparer<U>
    {
        private Func<U, T> contraMapper;
        private IEqualityComparer<T> comparer;

        public GenericEqualityComparer(Func<U, T> contraMapper, IEqualityComparer<T> comparer)
        {
            if (contraMapper == null) throw new ArgumentNullException(nameof(contraMapper));
            if (comparer == null) throw new ArgumentNullException(nameof(comparer));
            this.contraMapper = contraMapper;
            this.comparer = comparer;
        }

        public bool Equals(U x, U y) => comparer.Equals(contraMapper(x), contraMapper(y));
        public int GetHashCode(U obj) => comparer.GetHashCode(contraMapper(obj));
    }
}
