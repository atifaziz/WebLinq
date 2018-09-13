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

    public interface IFormSubmission<out T>
    {
        T Run(NameValueCollection data);
    }

    public interface IFormSubmissionAction<out T> : IFormSubmission<Unit>
    {
        IFormSubmission<T> Return();
    }

    static class FormSubmissionAction
    {
        public static FormSubmissionAction<T> Create<T>(IFormSubmission<T> submission) =>
            new FormSubmissionAction<T>(submission);
    }

    public sealed class FormSubmissionAction<T> : IFormSubmissionAction<T>
    {
        readonly IFormSubmission<T> _submission;

        public FormSubmissionAction(IFormSubmission<T> submission) =>
            _submission = submission ?? throw new ArgumentNullException(nameof(submission));

        public Unit Run(NameValueCollection data)
        {
            _submission.Run(data);
            return Unit.Default;
        }

        public IFormSubmission<T> Return() => _submission;
    }

    public static partial class FormSubmission
    {
        public static IFormSubmission<T> Create<T>(Func<NameValueCollection, T> runner) =>
            new DelegatingFormSubmission<T>(runner);

        sealed class DelegatingFormSubmission<T> : IFormSubmission<T>
        {
            readonly Func<NameValueCollection, T> _runner;

            public DelegatingFormSubmission(Func<NameValueCollection, T> runner) =>
                _runner = runner ?? throw new ArgumentNullException(nameof(runner));

            public T Run(NameValueCollection data) =>
                _runner(data ?? throw new ArgumentNullException(nameof(data)));
        }

        public static IFormSubmission<T> Return<T>(T value) => Create(_ => value);

        public static IFormSubmission<TResult> Bind<T, TResult>(this IFormSubmission<T> submission, Func<T, IFormSubmission<TResult>> selector) =>
            Create(env => selector(submission.Run(env)).Run(env));

        public static IFormSubmission<TResult> Select<T, TResult>(this IFormSubmission<T> submission, Func<T, TResult> selector) =>
            Create(env => selector(submission.Run(env)));

        public static IFormSubmission<TResult> SelectMany<TFirst, TSecond, TResult>(
            this IFormSubmission<TFirst> submission,
            Func<TFirst, IFormSubmission<TSecond>> secondSelector,
            Func<TFirst, TSecond, TResult> resultSelector) =>
            Create(env =>
            {
                var t = submission.Run(env);
                return resultSelector(t, secondSelector(t).Run(env));
            });

        public static IFormSubmission<TResult> SelectMany<T, TResult>(
            this IFormSubmission<T> submission,
            Func<T, IFormSubmission<TResult>> resultSelector) =>
            submission.SelectMany(resultSelector, (_, r) => r);

        public static IFormSubmission<IEnumerable<TResult>> For<T, TResult>(IEnumerable<T> source,
            Func<T, IFormSubmission<TResult>> f) =>
            Create(data => source.Select(f).Select(e => e.Run(data)).ToList());

        internal static IFormSubmission<T> Do<T>(this IFormSubmission<T> submission, Action<NameValueCollection> action) =>
            submission.Bind(x => Create(env => { action(env); return x; }));

        internal static IFormSubmission<Unit> Do(Action<NameValueCollection> action) =>
            Create(env => { action(env); return Unit.Default; });
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

        public static IFormSubmission<IReadOnlyCollection<string>> Names() =>
            Create(data => data.AllKeys);

        /// <summary>
        /// Gets the value of a field identified by its name.
        /// </summary>

        public static IFormSubmission<string> Get(string name) =>
            Create(data => data[name]);

        /// <summary>
        /// Gets all the values of a field identified by its name.
        /// </summary>

        public static IFormSubmission<IReadOnlyCollection<string>> GetValues(string name) =>
            Create(data => data.GetValues(name));

        /// <summary>
        /// Removes a field from submission.
        /// </summary>

        public static IFormSubmission<Unit> Remove(string name) =>
            Do(data => data.Remove(name));

        /// <summary>
        /// Sets the value of a field identified by its name.
        /// </summary>

        public static IFormSubmission<Unit> Set(string name, string value) =>
            Do(data => data[name] = value);

        /// <summary>
        /// Sets the values of all fields identified by a collection of
        /// names to the same value.
        /// </summary>

        public static IFormSubmission<Unit> Set(IEnumerable<string> names, string value) =>
            from _ in For(names, n => Set(n, value))
            select Unit.Default;

        static IFormSubmissionAction<string> TrySet(Func<IEnumerable<string>, string> matcher, string value) =>
            FormSubmissionAction.Create(
                from ns in Names()
                select matcher(ns) into n
                from r in n != null
                        ? from _ in Set(n, value) select n
                        : Return((string) null)
                select r);

        /// <summary>
        /// Sets the value of a single field identified by a predicate function
        /// otherwise throws an error.
        /// </summary>
        /// <returns>
        /// The name of the field whose value was set.
        /// </returns>

        public static IFormSubmissionAction<string> SetSingleWhere(Func<string, bool> matcher, string value) =>
            TrySet(ns => ns.Single(matcher), value);

        /// <summary>
        /// Sets the value of a single field identified by a regular expression
        /// pattern otherwise throws an error.
        /// </summary>
        /// <returns>
        /// The name of the field whose value was set.
        /// </returns>

        public static IFormSubmissionAction<string> SetSingleMatching(string pattern, string value) =>
            SetSingleWhere(n => Regex.IsMatch(n, pattern), value);

        /// <summary>
        /// Attempts to set the value of a single field identified by a
        /// predicate function otherwise has no effect.
        /// </summary>
        /// <returns>
        /// The name of the field whose value was set or <c>null</c> if zero
        /// or multiple fields were identified.
        /// </returns>

        public static IFormSubmissionAction<string> TrySetSingleWhere(Func<string, bool> matcher, string value) =>
            TrySet(ns => ns.SingleOrDefault(matcher), value);

        /// <summary>
        /// Attempts to Set the value of a single field identified by a
        /// regular expression pattern otherwise has no effect.
        /// </summary>
        /// <returns>
        /// The name of the field whose value was set or <c>null</c> if zero
        /// or multiple fields were identified.
        /// </returns>

        public static IFormSubmissionAction<string> TrySetSingleMatching(string pattern, string value) =>
            TrySetSingleWhere(n => Regex.IsMatch(n, pattern), value);

        /// <summary>
        /// Sets the value of the first field identified by a predicate
        /// function otherwise throws an error if no field was identified.
        /// </summary>
        /// <returns>
        /// The name of the first field identified by the predicate function.
        /// </returns>

        public static IFormSubmissionAction<string> SetFirstWhere(Func<string, bool> matcher, string value) =>
            TrySet(ns => ns.First(matcher), value);

        /// <summary>
        /// Attempts to set the value of the first field identified by a
        /// predicate function otherwise has no effect if no field was
        /// identified.
        /// </summary>
        /// <returns>
        /// The name of the first field identified by the predicate function.
        /// </returns>

        public static IFormSubmissionAction<string> TrySetFirstWhere(Func<string, bool> matcher, string value) =>
            TrySet(ns => ns.FirstOrDefault(matcher), value);

        /// <summary>
        /// Attempts to set the value of the first field identified by a
        /// regular expression pattern otherwise has no effect if no field was
        /// identified.
        /// </summary>
        /// <returns>
        /// The name of the first field identified by the predicate function.
        /// </returns>

        public static IFormSubmissionAction<string> TrySetFirstMatching(string pattern, string value) =>
            TrySetFirstWhere(n => Regex.IsMatch(n, pattern), value);

        /// <summary>
        /// Sets the values of all fields identified by a predicate function
        /// to the same value.
        /// </summary>
        /// <returns>
        /// A sequence of field names that were identified by the predicate
        /// function and affected.
        /// </returns>

        public static IFormSubmissionAction<IEnumerable<string>> SetWhere(Func<string, bool> matcher, string value) =>
            FormSubmissionAction.Create(
                from ns in Names()
                select ns.Where(matcher).ToArray() into ns
                from _ in Set(ns, value)
                select ns);

        /// <summary>
        /// Sets the values of all fields identified by a regular expression
        /// pattern to the same value.
        /// </summary>
        /// <returns>
        /// A sequence of field names that matched and were affected.
        /// </returns>

        public static IFormSubmissionAction<IEnumerable<string>> SetMatching(string pattern, string value) =>
            SetWhere(n => Regex.IsMatch(n, pattern), value);

        public static IFormSubmission<Unit> Merge(NameValueCollection other) =>
            Do(data =>
            {
                var entries = from e in other.AsEnumerable()
                              from v in e.Value select e.Key.AsKeyTo(v);
                foreach (var e in entries)
                    data.Add(e.Key, e.Value);
            });

        /// <summary>
        /// Returns a copy of the form data as a
        /// <see cref="NameValueCollection"/>.
        /// </summary>

        public static IFormSubmission<NameValueCollection> Data() =>
            Create(data => new NameValueCollection(data));

        /// <summary>
        /// Clears all form data.
        /// </summary>

        public static IFormSubmission<Unit> Clear() =>
            Do(data => data.Clear());

        /// <summary>
        /// Changes the type of the submission to <seealso cref="Unit"/>.
        /// </summary>

        public static IFormSubmission<Unit> Ignore<T>(this IFormSubmission<T> submission) =>
            from _ in submission
            select Unit.Default;

        /// <summary>
        /// Continues one submission after another.
        /// </summary>

        public static IFormSubmission<T> Then<T>(this IFormSubmission<Unit> first, IFormSubmission<T> second) =>
            from _ in first
            from b in second
            select b;

        /// <summary>
        /// Combines the result of one submission with another.
        /// </summary>

        public static IFormSubmission<TResult>
            Zip<TFirst, TSecond, TResult>(
                this IFormSubmission<TFirst> first,
                IFormSubmission<TSecond> second,
                Func<TFirst, TSecond, TResult> resultSelector) =>
            from a in first
            from b in second
            select resultSelector(a, b);

        public static IFormSubmission<Unit> Collect(params IFormSubmission<Unit>[] submissions) =>
            submissions.AsEnumerable().Collect();

        public static IFormSubmission<Unit> Collect(this IEnumerable<IFormSubmission<Unit>> submissions) =>
            For(submissions, s => s).Ignore();
    }

    partial class FormSubmission
    {
        static IFormSubmissionAction<string> TrySet(this HtmlForm form, Func<IEnumerable<string>, string> matcher, string value) =>
            FormSubmissionAction.Create(
                from ns in Return(from c in form.Controls select c.Name)
                select matcher(ns) into n
                from r in n != null
                    ? from _ in Set(n, value) select n
                    : Return((string) null)
                select r);

        /// <summary>
        /// Sets the value of a single field identified by a predicate function
        /// otherwise throws an error.
        /// </summary>
        /// <returns>
        /// The name of the field whose value was set.
        /// </returns>

        public static IFormSubmissionAction<string> SetSingleWhere(this HtmlForm form, Func<string, bool> matcher, string value) =>
            form.TrySet(ns => ns.Single(matcher), value);

        /// <summary>
        /// Sets the value of a single field identified by a regular expression
        /// pattern otherwise throws an error.
        /// </summary>
        /// <returns>
        /// The name of the field whose value was set.
        /// </returns>

        public static IFormSubmissionAction<string> SetSingleMatching(this HtmlForm form, string pattern, string value) =>
            form.SetSingleWhere(n => Regex.IsMatch(n, pattern), value);

        /// <summary>
        /// Attempts to set the value of a single field identified by a
        /// predicate function otherwise has no effect.
        /// </summary>
        /// <returns>
        /// The name of the field whose value was set or <c>null</c> if zero
        /// or multiple fields were identified.
        /// </returns>

        public static IFormSubmissionAction<string> TrySetSingleWhere(this HtmlForm form, Func<string, bool> matcher, string value) =>
            form.TrySet(ns => ns.SingleOrDefault(matcher), value);

        /// <summary>
        /// Attempts to Set the value of a single field identified by a
        /// regular expression pattern otherwise has no effect.
        /// </summary>
        /// <returns>
        /// The name of the field whose value was set or <c>null</c> if zero
        /// or multiple fields were identified.
        /// </returns>

        public static IFormSubmissionAction<string> TrySetSingleMatching(this HtmlForm form, string pattern, string value) =>
            form.TrySetSingleWhere(n => Regex.IsMatch(n, pattern), value);

        /// <summary>
        /// Sets the value of the first field identified by a predicate
        /// function otherwise throws an error if no field was identified.
        /// </summary>
        /// <returns>
        /// The name of the first field identified by the predicate function.
        /// </returns>

        public static IFormSubmissionAction<string> SetFirstWhere(this HtmlForm form, Func<string, bool> matcher, string value) =>
            form.TrySet(ns => ns.First(matcher), value);

        /// <summary>
        /// Attempts to set the value of the first field identified by a
        /// predicate function otherwise has no effect if no field was
        /// identified.
        /// </summary>
        /// <returns>
        /// The name of the first field identified by the predicate function.
        /// </returns>

        public static IFormSubmissionAction<string> TrySetFirstWhere(this HtmlForm form, Func<string, bool> matcher, string value) =>
            form.TrySet(ns => ns.FirstOrDefault(matcher), value);

        /// <summary>
        /// Attempts to set the value of the first field identified by a
        /// regular expression pattern otherwise has no effect if no field was
        /// identified.
        /// </summary>
        /// <returns>
        /// The name of the first field identified by the predicate function.
        /// </returns>

        public static IFormSubmissionAction<string> TrySetFirstMatching(this HtmlForm form, string pattern, string value) =>
            form.TrySetFirstWhere(n => Regex.IsMatch(n, pattern), value);

        /// <summary>
        /// Sets the values of all fields identified by a predicate function
        /// to the same value.
        /// </summary>
        /// <returns>
        /// A sequence of field names that were identified by the predicate
        /// function and affected.
        /// </returns>

        public static IFormSubmissionAction<IEnumerable<string>> SetWhere(this HtmlForm form, Func<string, bool> matcher, string value) =>
            FormSubmissionAction.Create(
                from ns in Return(from c in form.Controls select c.Name)
                select ns.Where(matcher).ToArray() into ns
                from _ in Set(ns, value)
                select ns);

        /// <summary>
        /// Sets the values of all fields identified by a regular expression
        /// pattern to the same value.
        /// </summary>
        /// <returns>
        /// A sequence of field names that matched and were affected.
        /// </returns>

        public static IFormSubmissionAction<IEnumerable<string>> SetMatching(this HtmlForm form, string pattern, string value) =>
            form.SetWhere(n => Regex.IsMatch(n, pattern), value);
    }
}
