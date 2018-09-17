#region Copyright (c) 2015 Atif Aziz. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

using System;

static class Choosing
{
    public static Func<ChoiceOf1<T1>, TResult> When1<T1, TResult>(Func<T1, TResult> selector) =>
        choice => choice.Match(selector);

    public static Func<ChoiceOf2<T1, T2>, TResult> ThenWhen2<T1, T2, TResult>(this Func<ChoiceOf1<T1>, TResult> otherwise, Func<T2, TResult> selector) =>
        choice => choice.Match(first => otherwise(ChoiceOf1<T1>.Choice1(first)), selector);

    public static Func<ChoiceOf3<T1, T2, T3>, TResult> ThenWhen3<T1, T2, T3, TResult>(this Func<ChoiceOf2<T1, T2>, TResult> otherwise, Func<T3, TResult> selector) =>
        choice => choice.Match(first => otherwise(ChoiceOf2<T1, T2>.Choice1(first)),
                               second => otherwise(ChoiceOf2<T1, T2>.Choice2(second)),
                               selector);
}

abstract class ChoiceOf1<T>
{
    public static ChoiceOf1<T> Choice1(T value) => new Choice1Of1(value);

    public abstract TResult Match<TResult>(Func<T, TResult> selector);

    sealed class Choice1Of1 : ChoiceOf1<T>
    {
        readonly T _value;
        public Choice1Of1(T value) { _value = value; }
        public override TResult Match<TResult>(Func<T, TResult> selector) =>
            selector(_value);
    }
}

abstract class ChoiceOf2<T1, T2>
{
    public static ChoiceOf2<T1, T2> Choice1(T1 value) => new Choice1Of2(value);
    public static ChoiceOf2<T1, T2> Choice2(T2 value) => new Choice2Of2(value);

    public abstract TResult Match<TResult>(Func<T1, TResult> selector1, Func<T2, TResult> selector2);

    sealed class Choice1Of2 : ChoiceOf2<T1, T2>
    {
        readonly T1 _value;
        public Choice1Of2(T1 value) { _value = value; }
        public override TResult Match<TResult>(Func<T1, TResult> selector1, Func<T2, TResult> selector2) =>
            selector1(_value);
    }

    sealed class Choice2Of2 : ChoiceOf2<T1, T2>
    {
        readonly T2 _value;
        public Choice2Of2(T2 value) { _value = value; }
        public override TResult Match<TResult>(Func<T1, TResult> selector1, Func<T2, TResult> selector2) =>
            selector2(_value);
    }
}

abstract class ChoiceOf3<T1, T2, T3>
{
    public static ChoiceOf3<T1, T2, T3> Choice1(T1 value) => new Choice1Of3(value);
    public static ChoiceOf3<T1, T2, T3> Choice2(T2 value) => new Choice2Of3(value);
    public static ChoiceOf3<T1, T2, T3> Choice3(T3 value) => new Choice3Of3(value);

    public abstract TResult Match<TResult>(
        Func<T1, TResult> selector1,
        Func<T2, TResult> selector2,
        Func<T3, TResult> selector3);

    sealed class Choice1Of3 : ChoiceOf3<T1, T2, T3>
    {
        readonly T1 _value;
        public Choice1Of3(T1 value) { _value = value; }
        public override TResult Match<TResult>(Func<T1, TResult> selector1, Func<T2, TResult> selector2, Func<T3, TResult> selector3) =>
            selector1(_value);
    }

    sealed class Choice2Of3 : ChoiceOf3<T1, T2, T3>
    {
        readonly T2 _value;
        public Choice2Of3(T2 value) { _value = value; }
        public override TResult Match<TResult>(Func<T1, TResult> selector1, Func<T2, TResult> selector2, Func<T3, TResult> selector3) =>
            selector2(_value);
    }

    sealed class Choice3Of3 : ChoiceOf3<T1, T2, T3>
    {
        readonly T3 _value;
        public Choice3Of3(T3 value) { _value = value; }
        public override TResult Match<TResult>(Func<T1, TResult> selector1, Func<T2, TResult> selector2, Func<T3, TResult> selector3) =>
            selector3(_value);
    }
}