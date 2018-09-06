#region Copyright (c) 2017 Atif Aziz. All rights reserved.
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
    using System.Collections.Specialized;
    using System.Linq;
    using System.Reactive;
    using Html;

    public sealed class FormSubmissionContext
    {
        public FormSubmissionContext(HtmlForm form, NameValueCollection data)
        {
            Form = form ?? throw new ArgumentNullException(nameof(form));
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public HtmlForm Form { get; }
        public NameValueCollection Data { get; }
    }

    public delegate T FormSubmission<out T>(FormSubmissionContext context);

    public static partial class FormSubmission
    {
        public static FormSubmission<T> Return<T>(T value) => _ => value;

        public static FormSubmission<TResult> Bind<T, TResult>(this FormSubmission<T> submission, Func<T, FormSubmission<TResult>> selector) =>
            env => selector(submission(env))(env);

        public static FormSubmission<TResult> Select<T, TResult>(this FormSubmission<T> submission, Func<T, TResult> selector) =>
            env => selector(submission(env));

        public static FormSubmission<TResult> SelectMany<TFirst, TSecond, TResult>(
            this FormSubmission<TFirst> submission,
            Func<TFirst, FormSubmission<TSecond>> secondSelector,
            Func<TFirst, TSecond, TResult> resultSelector) =>
            env =>
            {
                var t = submission(env);
                return resultSelector(t, secondSelector(t)(env));
            };

        public static FormSubmission<IEnumerable<TResult>> For<T, TResult>(IEnumerable<T> source,
            Func<T, FormSubmission<TResult>> f) =>
            context => source.Select(f).Select(e => e(context)).ToList();

        internal static FormSubmission<T> Do<T>(this FormSubmission<T> submission, Action<FormSubmissionContext> action) =>
            submission.Bind<T, T>(x => env => { action(env); return x; });

        internal static FormSubmission<Unit> Do(Action<FormSubmissionContext> action) =>
            env => { action(env); return Unit.Default; };
    }
}

namespace WebLinq
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Mannex.Collections.Generic;
    using Mannex.Collections.Specialized;
    using Unit = System.Reactive.Unit;

    public static partial class FormSubmission
    {
        public static FormSubmission<IReadOnlyCollection<string>> Keys() =>
            context => context.Data.AllKeys;

        public static FormSubmission<string> Get(string key) =>
            context => context.Data[key];

        public static FormSubmission<Unit> Set(string key, string value) =>
            Do(context => context.Data[key] = value);

        public static FormSubmission<Unit> Set(IEnumerable<string> keys, string value) =>
            from _ in For(keys, k => Set(k, value))
            select Unit.Default;

        static FormSubmission<string> TrySet(Func<IEnumerable<string>, string> matcher, string value) =>
            from ks in Keys()
            select matcher(ks) into k
            from r in k != null
                    ? from _ in Set(k, value) select k
                    : Return((string) null)
            select r;

        public static FormSubmission<string> SetSingleWhere(Func<string, bool> matcher, string value) =>
            TrySet(ks => ks.Single(matcher), value);

        public static FormSubmission<string> SetSingleMatching(string pattern, string value) =>
            SetSingleWhere(k => Regex.IsMatch(k, pattern), value);

        public static FormSubmission<string> TrySetSingleWhere(Func<string, bool> matcher, string value) =>
            TrySet(ks => ks.SingleOrDefault(matcher), value);

        public static FormSubmission<string> TrySetSingleMatching(string pattern, string value) =>
            TrySetSingleWhere(k => Regex.IsMatch(k, pattern), value);

        public static FormSubmission<string> SetFirstWhere(Func<string, bool> matcher, string value) =>
            TrySet(ks => ks.First(matcher), value);

        public static FormSubmission<string> TrySetFirstWhere(Func<string, bool> matcher, string value) =>
            TrySet(ks => ks.FirstOrDefault(matcher), value);

        public static FormSubmission<string> TrySetFirstMatching(string pattern, string value) =>
            TrySetFirstWhere(k => Regex.IsMatch(k, pattern), value);

        public static FormSubmission<IEnumerable<string>> SetWhere(Func<string, bool> matcher, string value) =>
            from ks in Keys()
            select ks.Where(matcher).ToArray() into ks
            from _ in Set(ks, value)
            select ks;

        public static FormSubmission<IEnumerable<string>> SetMatching(string pattern, string value) =>
            SetWhere(k => Regex.IsMatch(k, pattern), value);

        public static FormSubmission<Unit> Merge(NameValueCollection other) =>
            Do(context =>
            {
                var entries = from e in other.AsEnumerable()
                              from v in e.Value select e.Key.AsKeyTo(v);
                foreach (var e in entries)
                    context.Data.Add(e.Key, e.Value);
            });

        public static FormSubmission<NameValueCollection> Collect() =>
            context => new NameValueCollection(context.Data);

        public static FormSubmission<Unit> Clear() =>
            Do(context => context.Data.Clear());

        public static FormSubmission<Unit> Ignore<T>(this FormSubmission<T> submission) =>
            from _ in submission
            select Unit.Default;

        public static FormSubmission<T> Then<T>(this FormSubmission<Unit> first, FormSubmission<T> second) =>
            from _ in first
            from b in second
            select b;

        public static FormSubmission<Unit> Collect(params FormSubmission<Unit>[] submissions) =>
            submissions.AsEnumerable().Collect();

        public static FormSubmission<Unit> Collect(this IEnumerable<FormSubmission<Unit>> submissions) =>
            context =>
            {
                foreach (var other in submissions)
                    other(context);
                return Unit.Default;
            };
    }
}