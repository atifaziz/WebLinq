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

    sealed class TypedValue<T, TValue> : IEquatable<TypedValue<T, TValue>>
    {
        public TypedValue(TValue value) { Value = value; }

        public TValue Value { get; }

        public bool Equals(TypedValue<T, TValue> other) =>
            !ReferenceEquals(null, other)
            && (ReferenceEquals(this, other) || EqualityComparer<TValue>.Default.Equals(Value, other.Value));

        public override bool Equals(object obj) =>
            Equals(obj as TypedValue<T, TValue>);

        public override int GetHashCode() =>
            EqualityComparer<TValue>.Default.GetHashCode(Value);

        public override string ToString() => $"{Value}";
    }
}