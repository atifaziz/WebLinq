// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace WebLinq.Collections
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    partial struct Strings
    {
        public static Strings Array(params string[] values) =>
            new Strings(ImmutableArray.CreateRange(values));

        public static Strings Sequence(IEnumerable<string> values)
            => values is Strings strings
             ? strings
             : new Strings(ImmutableArray.CreateRange(values));
    }

    // Source:
    // https://github.com/aspnet/Extensions/blob/7ce647cfa3287e31497b72643eee28531eed1b7f/src/Primitives/src/StringValues.cs
    //
    // This is a slightly modified version from the snapshot above with the
    // following changes:
    //
    // - Moved from namespace Microsoft.Extensions.Primitives to one belonging
    //   to this project.
    // - Renamed from StringValues to Strings.
    // - Marked partial.
    // - Re-styled to use project conventions.
    // - Rendered single null string to be consistent with a single array of
    //   null string.

    /// <summary>
    /// Represents zero/null, one, or many strings in an efficient way.
    /// </summary>

    public readonly partial struct Strings :
        IList<string>,
        IReadOnlyList<string>,
        IEquatable<Strings>,
        IEquatable<string>
    {
#pragma warning disable CA1805 // Do not initialize unnecessarily
        public static readonly Strings Empty = new();
#pragma warning restore CA1805 // Do not initialize unnecessarily

        readonly string? _value;
        readonly ImmutableArray<string> _values;

        public Strings(string value) :
            this(value, default) { }

        public Strings(ImmutableArray<string> values) :
            this(null, values) { }

        Strings(string? value, ImmutableArray<string> values)
        {
            _value = value;
            _values = values.IsDefault && value is null
                    ? ImmutableArray<string>.Empty
                    : values;
        }

#pragma warning disable CA2225 // Operator overloads have named alternates

        public static implicit operator Strings(string value) => new(value);
        public static implicit operator Strings(ImmutableArray<string> values) => new(values);
        public static implicit operator string?(Strings values) => values.GetStringValue();
        public static implicit operator string[](Strings value) => value.GetArrayValue();

#pragma warning restore CA2225 // Operator overloads have named alternates

        public int Count => _value is not null ? 1
                          : _values.IsDefault ? 0
                          : _values.Length;

        bool ICollection<string>.IsReadOnly => true;

        string IList<string>.this[int index]
        {
            get => this[index];
            set => throw new NotSupportedException();
        }

        public string this[int index]
            => !_values.IsDefault ? _values[index]
             : index == 0 && _value is { } value ? value
             : System.Array.Empty<string>()[0]; // throws "IndexOutOfRangeException"

        public override string ToString() =>
            GetStringValue() ?? string.Empty;

        string? GetStringValue()
        {
            if (_values.IsDefault)
                return _value;

            switch (_values.Length)
            {
                case 0: return null;
                case 1: return _values[0];
                default: return string.Join(",", _values);
            }
        }

        public string[] ToArray() =>
            GetArrayValue() ?? System.Array.Empty<string>();

        string[] GetArrayValue()
        {
            if (_value is { } value)
                return new[] { value };

            if (_values.IsDefault)
                return System.Array.Empty<string>();

            var array = new string[Count];
            _values.CopyTo(array, 0);
            return array;
        }

        int IList<string>.IndexOf(string item) =>
            IndexOf(item);

        int IndexOf(string item)
        {
            if (!_values.IsDefault)
            {
                var values = _values;
                for (var i = 0; i < values.Length; i++)
                {
                    if (string.Equals(values[i], item, StringComparison.Ordinal))
                        return i;
                }

                return -1;
            }

            return _value is { } value
                 ? string.Equals(value, item, StringComparison.Ordinal) ? 0 : -1
                 : -1;
        }

        bool ICollection<string>.Contains(string item) =>
            IndexOf(item) >= 0;

        void ICollection<string>.CopyTo(string[] array, int arrayIndex) =>
            CopyTo(array, arrayIndex);

        void CopyTo(string[] array, int arrayIndex)
        {
            if (!_values.IsDefault)
            {
                _values.CopyTo(array, arrayIndex);
                return;
            }

            if (_value is { } value)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array));
                if (arrayIndex < 0)
                    throw new ArgumentOutOfRangeException(nameof(arrayIndex));

                if (array.Length - arrayIndex < 1)
                {
                    throw new ArgumentException(
                        $"'{nameof(array)}' is not long enough to copy all the items in the collection. Check '{nameof(arrayIndex)}' and '{nameof(array)}' length.");
                }

                array[arrayIndex] = value;
            }
        }

        void ICollection<string>.Add(string item) => throw new NotSupportedException();
        void IList<string>.Insert(int index, string item) => throw new NotSupportedException();
        bool ICollection<string>.Remove(string item) => throw new NotSupportedException();
        void IList<string>.RemoveAt(int index) => throw new NotSupportedException();
        void ICollection<string>.Clear() => throw new NotSupportedException();

        public Enumerator GetEnumerator() => new(_values, _value, Count);

        IEnumerator<string> IEnumerable<string>.GetEnumerator() =>
            GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();

        public static bool IsNullOrEmpty(Strings value) =>
            value.Count == 0 || string.IsNullOrEmpty(value[0]);

        public static Strings Concat(Strings values1, Strings values2)
        {
            var count1 = values1.Count;
            var count2 = values2.Count;

            if (count1 == 0)
                return values2;

            if (count2 == 0)
                return values1;

            var builder = ImmutableArray.CreateBuilder<string>(count1 + count2);
            for (var i = 0; i < count1; i++)
                builder.Add(values1[i]);
            for (var i = 0; i < count2; i++)
                builder.Add(values2[i]);
            return new Strings(builder.ToImmutable());
        }

        public static Strings Concat(in Strings values, string value)
        {
            var count = values.Count;
            if (count == 0)
                return new Strings(value);

            var builder = ImmutableArray.CreateBuilder<string>(count + 1);
            for (var i = 0; i < count; i++)
                builder.Add(values[i]);
            builder.Add(value);
            return new Strings(builder.ToImmutable());
        }

        public static Strings Concat(string value, in Strings values)
        {
            var count = values.Count;
            if (count == 0)
                return new Strings(value);

            var builder = ImmutableArray.CreateBuilder<string>(count + 1);
            builder.Add(value);
            for (var i = 0; i < count; i++)
                builder.Add(values[i]);
            return new Strings(builder.ToImmutable());
        }

        public static bool Equals(Strings left, Strings right)
        {
            var count = left.Count;

            if (count != right.Count)
                return false;

            for (var i = 0; i < count; i++)
            {
                if (left[i] != right[i])
                    return false;
            }

            return true;
        }

        public static bool operator ==(Strings left, Strings right) =>
            Equals(left, right);

        public static bool operator !=(Strings left, Strings right) =>
            !Equals(left, right);

        public bool Equals(Strings other) =>
            Equals(this, other);

        public static bool Equals(string left, Strings right) =>
            Equals(new Strings(left), right);

        public static bool Equals(Strings left, string right) =>
            Equals(left, new Strings(right));

        public bool Equals(string? other) =>
            other is { } someOther && Equals(this, new Strings(someOther));

        public static bool Equals(string[] left, Strings right) =>
            Equals(right, left);

        public static bool Equals(Strings left, string[] right)
        {
            var count = left.Count;

            if (right is null || count != right.Length)
                return false;

            for (var i = 0; i < count; i++)
            {
                if (left[i] != right[i])
                    return false;
            }

            return true;
        }

        public bool Equals(string[] other) =>
            Equals(this, other);

        public static bool operator ==(Strings left, string right) =>
            Equals(left, new Strings(right));

        public static bool operator !=(Strings left, string right) =>
            !Equals(left, new Strings(right));

        public static bool operator ==(string left, Strings right) =>
            Equals(new Strings(left), right);

        public static bool operator !=(string left, Strings right) =>
            !Equals(new Strings(left), right);

        public static bool operator ==(Strings left, string[] right) =>
            Equals(left, right);

        public static bool operator !=(Strings left, string[] right) =>
            !Equals(left, right);

        public static bool operator ==(string[] left, Strings right) =>
            Equals(left, right);

        public static bool operator !=(string[] left, Strings right) =>
            !Equals(left, right);

        public static bool operator ==(Strings left, object right) =>
            left.Equals(right);

        public static bool operator !=(Strings left, object right) =>
            !left.Equals(right);

        public static bool operator ==(object left, Strings right) =>
            right.Equals(left);

        public static bool operator !=(object left, Strings right) =>
            !right.Equals(left);

        public override bool Equals(object? obj)
        {
            switch (obj)
            {
                case null: return Equals(this, Empty);
                case string b: return Equals(this, b);
                case string[] b: return Equals(this, b);
                case Strings b: return Equals(this, b);
                default: return false;
            }
        }

        public override int GetHashCode()
        {
            if (_value is { } value)
                return value.GetHashCode(StringComparison.Ordinal);

            var hc = new HashCode();
            foreach (var v in _values)
                hc.Add(v);
            return hc.ToHashCode();
        }

        public struct Enumerator : IEnumerator<string>
        {
            readonly ImmutableArray<string> _values;
            string? _current;
            int _index;

            internal Enumerator(ImmutableArray<string> values, string? value, int count)
            {
               _values = values;
               _current = value;
               _index = count == 0 ? -1 : 0;
            }

            public bool MoveNext()
            {
                if (_index < 0)
                    return false;

                if (_values != null)
                {
                    if (_index < _values.Length)
                    {
                        _current = _values[_index];
                        _index++;
                        return true;
                    }

                    _index = -1;
                    return false;
                }

                _index = -1; // sentinel value
                return true;
            }

            public string Current => _current!;

            object IEnumerator.Current => Current;

            void IEnumerator.Reset() =>
                throw new NotSupportedException();

            public void Dispose() { }
        }
    }

    partial struct Strings
    {
        public bool Any() => Count > 0;

        public string First() =>
            Count > 0 ? this[0] : throw new InvalidOperationException();

        public string? FirstOrDefault() =>
            Count > 0 ? this[0] : null;

        int? LastIndex => Count > 0 ? Count - 1 : null;

        public string Last() =>
            LastIndex is { } i ? this[i] : throw new InvalidOperationException();

        public string? LastOrDefault() =>
            LastIndex is { } i ? this[i] : null;

#pragma warning disable CA1720 // Identifier contains type name
        public string Single() =>
#pragma warning restore CA1720 // Identifier contains type name
            Count == 1 ? this[0] : throw new InvalidOperationException();

        public string? SingleOrDefault() =>
            Count == 1 ? this[0] : null;
    }
}
