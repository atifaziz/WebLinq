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

        public static FormSubmission<TResult> SelectMany<T, TResult>(
            this FormSubmission<T> submission,
            Func<T, FormSubmission<TResult>> resultSelector) =>
            submission.SelectMany(resultSelector, (_, r) => r);

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
    using Html;
    using Mannex.Collections.Generic;
    using Mannex.Collections.Specialized;
    using Unit = System.Reactive.Unit;

    public static partial class FormSubmission
    {
        /// <summary>
        /// Get the names of all the fields.
        /// </summary>

        public static FormSubmission<IReadOnlyCollection<string>> Names() =>
            context => context.Data.AllKeys;

        /// <summary>
        /// Gets the parsed underlying HTML form.
        /// </summary>

        public static FormSubmission<HtmlForm> Form() => context => context.Form;

        /// <summary>
        /// Gets the value of a field identified by its name.
        /// </summary>

        public static FormSubmission<string> Get(string name) =>
            context => context.Data[name];

        /// <summary>
        /// Gets all the values of a field identified by its name.
        /// </summary>

        public static FormSubmission<IReadOnlyCollection<string>> GetValues(string name) =>
            context => context.Data.GetValues(name);

        /// <summary>
        /// Removes a field from submission.
        /// </summary>

        public static FormSubmission<Unit> Remove(string name) =>
            Do(context => context.Data.Remove(name));

        /// <summary>
        /// Sets the value of a field identified by its name.
        /// </summary>

        public static FormSubmission<Unit> Set(string name, string value) =>
            Do(context => context.Data[name] = value);

        /// <summary>
        /// Sets the values of all fields identified by a collection of
        /// names to the same value.
        /// </summary>

        public static FormSubmission<Unit> Set(IEnumerable<string> names, string value) =>
            from _ in For(names, n => Set(n, value))
            select Unit.Default;

        static FormSubmission<string> TrySet(Func<IEnumerable<string>, string> matcher, string value) =>
            from ns in Names()
            select matcher(ns) into n
            from r in n != null
                    ? from _ in Set(n, value) select n
                    : Return((string) null)
            select r;

        /// <summary>
        /// Sets the value of a single field identified by a predicate function
        /// otherwise throws an error.
        /// </summary>
        /// <returns>
        /// The name of the field whose value was set.
        /// </returns>

        public static FormSubmission<string> SetSingleWhere(Func<string, bool> matcher, string value) =>
            TrySet(ns => ns.Single(matcher), value);

        /// <summary>
        /// Sets the value of a single field identified by a regular expression
        /// pattern otherwise throws an error.
        /// </summary>
        /// <returns>
        /// The name of the field whose value was set.
        /// </returns>

        public static FormSubmission<string> SetSingleMatching(string pattern, string value) =>
            SetSingleWhere(n => Regex.IsMatch(n, pattern), value);

        /// <summary>
        /// Attempts to set the value of a single field identified by a
        /// predicate function otherwise has no effect.
        /// </summary>
        /// <returns>
        /// The name of the field whose value was set or <c>null</c> if zero
        /// or multiple fields were identified.
        /// </returns>

        public static FormSubmission<string> TrySetSingleWhere(Func<string, bool> matcher, string value) =>
            TrySet(ns => ns.SingleOrDefault(matcher), value);

        /// <summary>
        /// Attempts to Set the value of a single field identified by a
        /// regular expression pattern otherwise has no effect.
        /// </summary>
        /// <returns>
        /// The name of the field whose value was set or <c>null</c> if zero
        /// or multiple fields were identified.
        /// </returns>

        public static FormSubmission<string> TrySetSingleMatching(string pattern, string value) =>
            TrySetSingleWhere(n => Regex.IsMatch(n, pattern), value);

        /// <summary>
        /// Sets the value of the first field identified by a predicate
        /// function otherwise throws an error if no field was identified.
        /// </summary>
        /// <returns>
        /// The name of the first field identified by the predicate function.
        /// </returns>

        public static FormSubmission<string> SetFirstWhere(Func<string, bool> matcher, string value) =>
            TrySet(ns => ns.First(matcher), value);

        /// <summary>
        /// Attempts to set the value of the first field identified by a
        /// predicate function otherwise has no effect if no field was
        /// identified.
        /// </summary>
        /// <returns>
        /// The name of the first field identified by the predicate function.
        /// </returns>

        public static FormSubmission<string> TrySetFirstWhere(Func<string, bool> matcher, string value) =>
            TrySet(ns => ns.FirstOrDefault(matcher), value);

        /// <summary>
        /// Attempts to set the value of the first field identified by a
        /// regular expression pattern otherwise has no effect if no field was
        /// identified.
        /// </summary>
        /// <returns>
        /// The name of the first field identified by the predicate function.
        /// </returns>

        public static FormSubmission<string> TrySetFirstMatching(string pattern, string value) =>
            TrySetFirstWhere(n => Regex.IsMatch(n, pattern), value);

        /// <summary>
        /// Sets the values of all fields identified by a predicate function
        /// to the same value.
        /// </summary>
        /// <returns>
        /// A sequence of field names that were identified by the predicate
        /// function and affected.
        /// </returns>

        public static FormSubmission<IEnumerable<string>> SetWhere(Func<string, bool> matcher, string value) =>
            from ns in Names()
            select ns.Where(matcher).ToArray() into ns
            from _ in Set(ns, value)
            select ns;

        /// <summary>
        /// Sets the values of all fields identified by a regular expression
        /// pattern to the same value.
        /// </summary>
        /// <returns>
        /// A sequence of field names that matched and were affected.
        /// </returns>

        public static FormSubmission<IEnumerable<string>> SetMatching(string pattern, string value) =>
            SetWhere(n => Regex.IsMatch(n, pattern), value);

        public static FormSubmission<Unit> Merge(NameValueCollection other) =>
            Do(context =>
            {
                var entries = from e in other.AsEnumerable()
                              from v in e.Value select e.Key.AsKeyTo(v);
                foreach (var e in entries)
                    context.Data.Add(e.Key, e.Value);
            });

        /// <summary>
        /// Returns a copy of the form data as a
        /// <see cref="NameValueCollection"/>.
        /// </summary>

        public static FormSubmission<NameValueCollection> Data() =>
            context => new NameValueCollection(context.Data);

        /// <summary>
        /// Clears all form data.
        /// </summary>

        public static FormSubmission<Unit> Clear() =>
            Do(context => context.Data.Clear());

        /// <summary>
        /// Changes the type of the submission to <seealso cref="Unit"/>.
        /// </summary>

        public static FormSubmission<Unit> Ignore<T>(this FormSubmission<T> submission) =>
            from _ in submission
            select Unit.Default;

        /// <summary>
        /// Continues one submission after another.
        /// </summary>

        public static FormSubmission<T> Then<T>(this FormSubmission<Unit> first, FormSubmission<T> second) =>
            from _ in first
            from b in second
            select b;

        /// <summary>
        /// Combines the result of one submission with another.
        /// </summary>

        public static FormSubmission<TResult>
            Zip<TFirst, TSecond, TResult>(
                this FormSubmission<TFirst> first,
                FormSubmission<TSecond> second,
                Func<TFirst, TSecond, TResult> resultSelector) =>
            from a in first
            from b in second
            select resultSelector(a, b);

        public static FormSubmission<Unit> Collect(params FormSubmission<Unit>[] submissions) =>
            submissions.AsEnumerable().Collect();

        public static FormSubmission<Unit> Collect(this IEnumerable<FormSubmission<Unit>> submissions) =>
            For(submissions, s => s).Ignore();
    }
}
