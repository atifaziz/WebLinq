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

namespace WebLinq.Modules
{
    using System;
    using System.Collections.Generic;

    partial class XmlModule
    {
        public static IEnumerable<TResult> Xml<TNode1, T1,TNode2, T2, TResult>(string xml, string xpath, string xpath1, Func<TNode1, T1> selector1, string xpath2, Func<TNode2, T2> selector2, Func<T1, T2, TResult> resultSelector)
        {
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));
            return
                Xml(xml, xpath, 2,
                    xpath1, selector1, xpath2, selector2,
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    (a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p) =>
                        resultSelector(a, b));
        }
        public static IEnumerable<TResult> Xml<TNode1, T1,TNode2, T2,TNode3, T3, TResult>(string xml, string xpath, string xpath1, Func<TNode1, T1> selector1, string xpath2, Func<TNode2, T2> selector2, string xpath3, Func<TNode3, T3> selector3, Func<T1, T2, T3, TResult> resultSelector)
        {
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));
            return
                Xml(xml, xpath, 3,
                    xpath1, selector1, xpath2, selector2, xpath3, selector3,
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    (a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p) =>
                        resultSelector(a, b, c));
        }
        public static IEnumerable<TResult> Xml<TNode1, T1,TNode2, T2,TNode3, T3,TNode4, T4, TResult>(string xml, string xpath, string xpath1, Func<TNode1, T1> selector1, string xpath2, Func<TNode2, T2> selector2, string xpath3, Func<TNode3, T3> selector3, string xpath4, Func<TNode4, T4> selector4, Func<T1, T2, T3, T4, TResult> resultSelector)
        {
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));
            return
                Xml(xml, xpath, 4,
                    xpath1, selector1, xpath2, selector2, xpath3, selector3, xpath4, selector4,
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    (a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p) =>
                        resultSelector(a, b, c, d));
        }
        public static IEnumerable<TResult> Xml<TNode1, T1,TNode2, T2,TNode3, T3,TNode4, T4,TNode5, T5, TResult>(string xml, string xpath, string xpath1, Func<TNode1, T1> selector1, string xpath2, Func<TNode2, T2> selector2, string xpath3, Func<TNode3, T3> selector3, string xpath4, Func<TNode4, T4> selector4, string xpath5, Func<TNode5, T5> selector5, Func<T1, T2, T3, T4, T5, TResult> resultSelector)
        {
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));
            return
                Xml(xml, xpath, 5,
                    xpath1, selector1, xpath2, selector2, xpath3, selector3, xpath4, selector4, xpath5, selector5,
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    (a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p) =>
                        resultSelector(a, b, c, d, e));
        }
        public static IEnumerable<TResult> Xml<TNode1, T1,TNode2, T2,TNode3, T3,TNode4, T4,TNode5, T5,TNode6, T6, TResult>(string xml, string xpath, string xpath1, Func<TNode1, T1> selector1, string xpath2, Func<TNode2, T2> selector2, string xpath3, Func<TNode3, T3> selector3, string xpath4, Func<TNode4, T4> selector4, string xpath5, Func<TNode5, T5> selector5, string xpath6, Func<TNode6, T6> selector6, Func<T1, T2, T3, T4, T5, T6, TResult> resultSelector)
        {
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));
            return
                Xml(xml, xpath, 6,
                    xpath1, selector1, xpath2, selector2, xpath3, selector3, xpath4, selector4, xpath5, selector5, xpath6, selector6,
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    (a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p) =>
                        resultSelector(a, b, c, d, e, f));
        }
        public static IEnumerable<TResult> Xml<TNode1, T1,TNode2, T2,TNode3, T3,TNode4, T4,TNode5, T5,TNode6, T6,TNode7, T7, TResult>(string xml, string xpath, string xpath1, Func<TNode1, T1> selector1, string xpath2, Func<TNode2, T2> selector2, string xpath3, Func<TNode3, T3> selector3, string xpath4, Func<TNode4, T4> selector4, string xpath5, Func<TNode5, T5> selector5, string xpath6, Func<TNode6, T6> selector6, string xpath7, Func<TNode7, T7> selector7, Func<T1, T2, T3, T4, T5, T6, T7, TResult> resultSelector)
        {
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));
            return
                Xml(xml, xpath, 7,
                    xpath1, selector1, xpath2, selector2, xpath3, selector3, xpath4, selector4, xpath5, selector5, xpath6, selector6, xpath7, selector7,
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    (a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p) =>
                        resultSelector(a, b, c, d, e, f, g));
        }
        public static IEnumerable<TResult> Xml<TNode1, T1,TNode2, T2,TNode3, T3,TNode4, T4,TNode5, T5,TNode6, T6,TNode7, T7,TNode8, T8, TResult>(string xml, string xpath, string xpath1, Func<TNode1, T1> selector1, string xpath2, Func<TNode2, T2> selector2, string xpath3, Func<TNode3, T3> selector3, string xpath4, Func<TNode4, T4> selector4, string xpath5, Func<TNode5, T5> selector5, string xpath6, Func<TNode6, T6> selector6, string xpath7, Func<TNode7, T7> selector7, string xpath8, Func<TNode8, T8> selector8, Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> resultSelector)
        {
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));
            return
                Xml(xml, xpath, 8,
                    xpath1, selector1, xpath2, selector2, xpath3, selector3, xpath4, selector4, xpath5, selector5, xpath6, selector6, xpath7, selector7, xpath8, selector8,
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    (a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p) =>
                        resultSelector(a, b, c, d, e, f, g, h));
        }
        public static IEnumerable<TResult> Xml<TNode1, T1,TNode2, T2,TNode3, T3,TNode4, T4,TNode5, T5,TNode6, T6,TNode7, T7,TNode8, T8,TNode9, T9, TResult>(string xml, string xpath, string xpath1, Func<TNode1, T1> selector1, string xpath2, Func<TNode2, T2> selector2, string xpath3, Func<TNode3, T3> selector3, string xpath4, Func<TNode4, T4> selector4, string xpath5, Func<TNode5, T5> selector5, string xpath6, Func<TNode6, T6> selector6, string xpath7, Func<TNode7, T7> selector7, string xpath8, Func<TNode8, T8> selector8, string xpath9, Func<TNode9, T9> selector9, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> resultSelector)
        {
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));
            return
                Xml(xml, xpath, 9,
                    xpath1, selector1, xpath2, selector2, xpath3, selector3, xpath4, selector4, xpath5, selector5, xpath6, selector6, xpath7, selector7, xpath8, selector8, xpath9, selector9,
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    (a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p) =>
                        resultSelector(a, b, c, d, e, f, g, h, i));
        }
        public static IEnumerable<TResult> Xml<TNode1, T1,TNode2, T2,TNode3, T3,TNode4, T4,TNode5, T5,TNode6, T6,TNode7, T7,TNode8, T8,TNode9, T9,TNode10, T10, TResult>(string xml, string xpath, string xpath1, Func<TNode1, T1> selector1, string xpath2, Func<TNode2, T2> selector2, string xpath3, Func<TNode3, T3> selector3, string xpath4, Func<TNode4, T4> selector4, string xpath5, Func<TNode5, T5> selector5, string xpath6, Func<TNode6, T6> selector6, string xpath7, Func<TNode7, T7> selector7, string xpath8, Func<TNode8, T8> selector8, string xpath9, Func<TNode9, T9> selector9, string xpath10, Func<TNode10, T10> selector10, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> resultSelector)
        {
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));
            return
                Xml(xml, xpath, 10,
                    xpath1, selector1, xpath2, selector2, xpath3, selector3, xpath4, selector4, xpath5, selector5, xpath6, selector6, xpath7, selector7, xpath8, selector8, xpath9, selector9, xpath10, selector10,
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    (a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p) =>
                        resultSelector(a, b, c, d, e, f, g, h, i, j));
        }
        public static IEnumerable<TResult> Xml<TNode1, T1,TNode2, T2,TNode3, T3,TNode4, T4,TNode5, T5,TNode6, T6,TNode7, T7,TNode8, T8,TNode9, T9,TNode10, T10,TNode11, T11, TResult>(string xml, string xpath, string xpath1, Func<TNode1, T1> selector1, string xpath2, Func<TNode2, T2> selector2, string xpath3, Func<TNode3, T3> selector3, string xpath4, Func<TNode4, T4> selector4, string xpath5, Func<TNode5, T5> selector5, string xpath6, Func<TNode6, T6> selector6, string xpath7, Func<TNode7, T7> selector7, string xpath8, Func<TNode8, T8> selector8, string xpath9, Func<TNode9, T9> selector9, string xpath10, Func<TNode10, T10> selector10, string xpath11, Func<TNode11, T11> selector11, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult> resultSelector)
        {
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));
            return
                Xml(xml, xpath, 11,
                    xpath1, selector1, xpath2, selector2, xpath3, selector3, xpath4, selector4, xpath5, selector5, xpath6, selector6, xpath7, selector7, xpath8, selector8, xpath9, selector9, xpath10, selector10, xpath11, selector11,
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    (a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p) =>
                        resultSelector(a, b, c, d, e, f, g, h, i, j, k));
        }
        public static IEnumerable<TResult> Xml<TNode1, T1,TNode2, T2,TNode3, T3,TNode4, T4,TNode5, T5,TNode6, T6,TNode7, T7,TNode8, T8,TNode9, T9,TNode10, T10,TNode11, T11,TNode12, T12, TResult>(string xml, string xpath, string xpath1, Func<TNode1, T1> selector1, string xpath2, Func<TNode2, T2> selector2, string xpath3, Func<TNode3, T3> selector3, string xpath4, Func<TNode4, T4> selector4, string xpath5, Func<TNode5, T5> selector5, string xpath6, Func<TNode6, T6> selector6, string xpath7, Func<TNode7, T7> selector7, string xpath8, Func<TNode8, T8> selector8, string xpath9, Func<TNode9, T9> selector9, string xpath10, Func<TNode10, T10> selector10, string xpath11, Func<TNode11, T11> selector11, string xpath12, Func<TNode12, T12> selector12, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult> resultSelector)
        {
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));
            return
                Xml(xml, xpath, 12,
                    xpath1, selector1, xpath2, selector2, xpath3, selector3, xpath4, selector4, xpath5, selector5, xpath6, selector6, xpath7, selector7, xpath8, selector8, xpath9, selector9, xpath10, selector10, xpath11, selector11, xpath12, selector12,
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    (a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p) =>
                        resultSelector(a, b, c, d, e, f, g, h, i, j, k, l));
        }
        public static IEnumerable<TResult> Xml<TNode1, T1,TNode2, T2,TNode3, T3,TNode4, T4,TNode5, T5,TNode6, T6,TNode7, T7,TNode8, T8,TNode9, T9,TNode10, T10,TNode11, T11,TNode12, T12,TNode13, T13, TResult>(string xml, string xpath, string xpath1, Func<TNode1, T1> selector1, string xpath2, Func<TNode2, T2> selector2, string xpath3, Func<TNode3, T3> selector3, string xpath4, Func<TNode4, T4> selector4, string xpath5, Func<TNode5, T5> selector5, string xpath6, Func<TNode6, T6> selector6, string xpath7, Func<TNode7, T7> selector7, string xpath8, Func<TNode8, T8> selector8, string xpath9, Func<TNode9, T9> selector9, string xpath10, Func<TNode10, T10> selector10, string xpath11, Func<TNode11, T11> selector11, string xpath12, Func<TNode12, T12> selector12, string xpath13, Func<TNode13, T13> selector13, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult> resultSelector)
        {
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));
            return
                Xml(xml, xpath, 13,
                    xpath1, selector1, xpath2, selector2, xpath3, selector3, xpath4, selector4, xpath5, selector5, xpath6, selector6, xpath7, selector7, xpath8, selector8, xpath9, selector9, xpath10, selector10, xpath11, selector11, xpath12, selector12, xpath13, selector13,
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    (a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p) =>
                        resultSelector(a, b, c, d, e, f, g, h, i, j, k, l, m));
        }
        public static IEnumerable<TResult> Xml<TNode1, T1,TNode2, T2,TNode3, T3,TNode4, T4,TNode5, T5,TNode6, T6,TNode7, T7,TNode8, T8,TNode9, T9,TNode10, T10,TNode11, T11,TNode12, T12,TNode13, T13,TNode14, T14, TResult>(string xml, string xpath, string xpath1, Func<TNode1, T1> selector1, string xpath2, Func<TNode2, T2> selector2, string xpath3, Func<TNode3, T3> selector3, string xpath4, Func<TNode4, T4> selector4, string xpath5, Func<TNode5, T5> selector5, string xpath6, Func<TNode6, T6> selector6, string xpath7, Func<TNode7, T7> selector7, string xpath8, Func<TNode8, T8> selector8, string xpath9, Func<TNode9, T9> selector9, string xpath10, Func<TNode10, T10> selector10, string xpath11, Func<TNode11, T11> selector11, string xpath12, Func<TNode12, T12> selector12, string xpath13, Func<TNode13, T13> selector13, string xpath14, Func<TNode14, T14> selector14, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult> resultSelector)
        {
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));
            return
                Xml(xml, xpath, 14,
                    xpath1, selector1, xpath2, selector2, xpath3, selector3, xpath4, selector4, xpath5, selector5, xpath6, selector6, xpath7, selector7, xpath8, selector8, xpath9, selector9, xpath10, selector10, xpath11, selector11, xpath12, selector12, xpath13, selector13, xpath14, selector14,
                    null, default(Func<object, object>),
                    null, default(Func<object, object>),
                    (a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p) =>
                        resultSelector(a, b, c, d, e, f, g, h, i, j, k, l, m, n));
        }
        public static IEnumerable<TResult> Xml<TNode1, T1,TNode2, T2,TNode3, T3,TNode4, T4,TNode5, T5,TNode6, T6,TNode7, T7,TNode8, T8,TNode9, T9,TNode10, T10,TNode11, T11,TNode12, T12,TNode13, T13,TNode14, T14,TNode15, T15, TResult>(string xml, string xpath, string xpath1, Func<TNode1, T1> selector1, string xpath2, Func<TNode2, T2> selector2, string xpath3, Func<TNode3, T3> selector3, string xpath4, Func<TNode4, T4> selector4, string xpath5, Func<TNode5, T5> selector5, string xpath6, Func<TNode6, T6> selector6, string xpath7, Func<TNode7, T7> selector7, string xpath8, Func<TNode8, T8> selector8, string xpath9, Func<TNode9, T9> selector9, string xpath10, Func<TNode10, T10> selector10, string xpath11, Func<TNode11, T11> selector11, string xpath12, Func<TNode12, T12> selector12, string xpath13, Func<TNode13, T13> selector13, string xpath14, Func<TNode14, T14> selector14, string xpath15, Func<TNode15, T15> selector15, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult> resultSelector)
        {
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));
            return
                Xml(xml, xpath, 15,
                    xpath1, selector1, xpath2, selector2, xpath3, selector3, xpath4, selector4, xpath5, selector5, xpath6, selector6, xpath7, selector7, xpath8, selector8, xpath9, selector9, xpath10, selector10, xpath11, selector11, xpath12, selector12, xpath13, selector13, xpath14, selector14, xpath15, selector15,
                    null, default(Func<object, object>),
                    (a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p) =>
                        resultSelector(a, b, c, d, e, f, g, h, i, j, k, l, m, n, o));
        }
        public static IEnumerable<TResult> Xml<TNode1, T1,TNode2, T2,TNode3, T3,TNode4, T4,TNode5, T5,TNode6, T6,TNode7, T7,TNode8, T8,TNode9, T9,TNode10, T10,TNode11, T11,TNode12, T12,TNode13, T13,TNode14, T14,TNode15, T15,TNode16, T16, TResult>(string xml, string xpath, string xpath1, Func<TNode1, T1> selector1, string xpath2, Func<TNode2, T2> selector2, string xpath3, Func<TNode3, T3> selector3, string xpath4, Func<TNode4, T4> selector4, string xpath5, Func<TNode5, T5> selector5, string xpath6, Func<TNode6, T6> selector6, string xpath7, Func<TNode7, T7> selector7, string xpath8, Func<TNode8, T8> selector8, string xpath9, Func<TNode9, T9> selector9, string xpath10, Func<TNode10, T10> selector10, string xpath11, Func<TNode11, T11> selector11, string xpath12, Func<TNode12, T12> selector12, string xpath13, Func<TNode13, T13> selector13, string xpath14, Func<TNode14, T14> selector14, string xpath15, Func<TNode15, T15> selector15, string xpath16, Func<TNode16, T16> selector16, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult> resultSelector)
        {
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));
            return
                Xml(xml, xpath, 16,
                    xpath1, selector1, xpath2, selector2, xpath3, selector3, xpath4, selector4, xpath5, selector5, xpath6, selector6, xpath7, selector7, xpath8, selector8, xpath9, selector9, xpath10, selector10, xpath11, selector11, xpath12, selector12, xpath13, selector13, xpath14, selector14, xpath15, selector15, xpath16, selector16,
                resultSelector);
        }
    }
}
