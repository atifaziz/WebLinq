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
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;

    [DebuggerDisplay("Count = {Count}")]
    public sealed class Vars : IDictionary<string, object>
    {
        readonly IDictionary<string, object> _vars;

        public Vars() : this(null) {}

        public Vars(IEnumerable<KeyValuePair<string, object>> vars)
        {
            _vars = vars?.ToDictionary(e => e.Key, e => e.Value, StringComparer.OrdinalIgnoreCase)
                    ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        public int Count => _vars.Count;
        public ICollection<string> Keys => _vars.Keys;
        public ICollection<object> Values => _vars.Values;

        public object this[string name] { get { return _vars[name];  }
                                          set { _vars[name] = value; } }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _vars.GetEnumerator();

        public void Add(string key, object value) => _vars.Add(key, value);
        public bool TryGetValue(string key, out object value) => _vars.TryGetValue(key, out value);
        public bool Contains(string name) => _vars.ContainsKey(name);
        public bool Remove(string name) => _vars.Remove(name);
        public void Clear() => _vars.Clear();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) _vars).GetEnumerator();

        void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item) => _vars.Add(item);
        bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item) => _vars.Contains(item);
        void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) => _vars.CopyTo(array, arrayIndex);
        bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item) => _vars.Remove(item);
        bool ICollection<KeyValuePair<string, object>>.IsReadOnly => _vars.IsReadOnly;

        bool IDictionary<string, object>.ContainsKey(string key) => Contains(key);
        bool IDictionary<string, object>.Remove(string key) => Remove(key);
        object IDictionary<string, object>.this[string key] { get { return this[key];  }
                                                              set { this[key] = value; } }
    }

    public interface IVarService
    {
        Vars Vars { get; }
    }

    public static class VarQuery
    {
        public static Query<Vars> Vars() =>
            Query.Create(context => context.Eval((IVarService s) => QueryResult.Create(context, s.Vars)));

        public static Query<object> Var(string name) =>
            Var<object>(name);

        public static Query<T> Var<T>(string name) =>
            Vars().Bind(vars =>
            {
                object value;
                return vars.TryGetValue(name, out value)
                     ? Query.Return((T) value)
                     : Query<T>.Empty;
            });

        public static Query<TResult> Vars<T1, T2, TResult>(string name1, string name2, Func<T1, T2, TResult> selector) =>
            from a in Var<T1>(name1)
            from b in Var<T2>(name2)
            select selector(a, b);

        public static Query<TResult> Vars<T1, T2, T3, TResult>(string name1, string name2, string name3, Func<T1, T2, T3, TResult> selector) =>
            from a in Var<T1>(name1)
            from b in Var<T2>(name2)
            from c in Var<T3>(name3)
            select selector(a, b, c);

        public static Query<T> Var<T>(string name, T value) =>
            Vars().Bind(vars =>
            {
                vars[name] = value;
                return Query.Return(value);
            });

        public static Query<T> Swap<T>(string name, T value) =>
            Vars().Bind(vars =>
            {
                object old;
                var result =
                     vars.TryGetValue(name, out old)
                     ? Query.Return((T)old)
                     : Query<T>.Empty;
                vars[name] = value;
                return result;
            });
    }

    public class VarService : IVarService
    {
        public VarService() : this(null) {}
        public VarService(Vars vars) { Vars = vars ?? new Vars(); }

        public void Register(Action<Type, object> registrationHandler) =>
            registrationHandler(typeof(IVarService), this);

        public Vars Vars { get; }
    }
}