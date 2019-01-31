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

    public interface ISubmissionData<out T>
    {
        T Run(NameValueCollection data);
    }

    public interface ISubmissionDataAction<out T> : ISubmissionData<Unit>
    {
        ISubmissionData<T> Return();
    }

    static class FormSubmissionAction
    {
        public static SubmissionDataAction<T> Create<T>(ISubmissionData<T> submission) =>
            new SubmissionDataAction<T>(submission);
    }

    public sealed class SubmissionDataAction<T> : ISubmissionDataAction<T>
    {
        readonly ISubmissionData<T> _submission;

        public SubmissionDataAction(ISubmissionData<T> submission) =>
            _submission = submission ?? throw new ArgumentNullException(nameof(submission));

        public Unit Run(NameValueCollection data)
        {
            _submission.Run(data);
            return Unit.Default;
        }

        public ISubmissionData<T> Return() => _submission;
    }

    public static partial class SubmissionData
    {
        public static ISubmissionData<T> Create<T>(Func<NameValueCollection, T> runner) =>
            new DelegatingSubmission<T>(runner);

        sealed class DelegatingSubmission<T> : ISubmissionData<T>
        {
            readonly Func<NameValueCollection, T> _runner;

            public DelegatingSubmission(Func<NameValueCollection, T> runner) =>
                _runner = runner ?? throw new ArgumentNullException(nameof(runner));

            public T Run(NameValueCollection data) =>
                _runner(data ?? throw new ArgumentNullException(nameof(data)));
        }

        public static ISubmissionData<T> Return<T>(T value) => Create(_ => value);

        public static ISubmissionData<TResult> Bind<T, TResult>(this ISubmissionData<T> submission, Func<T, ISubmissionData<TResult>> selector) =>
            Create(env => selector(submission.Run(env)).Run(env));

        public static ISubmissionData<TResult> Select<T, TResult>(this ISubmissionData<T> submission, Func<T, TResult> selector) =>
            Create(env => selector(submission.Run(env)));

        public static ISubmissionData<TResult> SelectMany<TFirst, TSecond, TResult>(
            this ISubmissionData<TFirst> submission,
            Func<TFirst, ISubmissionData<TSecond>> secondSelector,
            Func<TFirst, TSecond, TResult> resultSelector) =>
            Create(env =>
            {
                var t = submission.Run(env);
                return resultSelector(t, secondSelector(t).Run(env));
            });

        public static ISubmissionData<TResult> SelectMany<T, TResult>(
            this ISubmissionData<T> submission,
            Func<T, ISubmissionData<TResult>> resultSelector) =>
            submission.SelectMany(resultSelector, (_, r) => r);

        public static ISubmissionData<IEnumerable<TResult>> For<T, TResult>(IEnumerable<T> source,
            Func<T, ISubmissionData<TResult>> f) =>
            Create(data => source.Select(f).Select(e => e.Run(data)).ToList());

        internal static ISubmissionData<T> Do<T>(this ISubmissionData<T> submission, Action<NameValueCollection> action) =>
            submission.Bind(x => Create(env => { action(env); return x; }));

        internal static ISubmissionData<Unit> Do(Action<NameValueCollection> action) =>
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
    using Mannex.Collections.Generic;
    using Mannex.Collections.Specialized;
    using Unit = System.Reactive.Unit;

    public static partial class SubmissionData
    {
        /// <summary>
        /// Represents nothing.
        /// </summary>

        public static ISubmissionData<Unit> None = Do(delegate { });

        /// <summary>
        /// Get the names of all the fields.
        /// </summary>

        public static ISubmissionData<IReadOnlyCollection<string>> Names() =>
            Create(data => data.AllKeys);

        /// <summary>
        /// Gets the value of a field identified by its name.
        /// </summary>

        public static ISubmissionData<string> Get(string name) =>
            Create(data => data[name]);

        /// <summary>
        /// Gets all the values of a field identified by its name.
        /// </summary>

        public static ISubmissionData<IReadOnlyCollection<string>> GetValues(string name) =>
            Create(data => data.GetValues(name));

        /// <summary>
        /// Removes a field from submission.
        /// </summary>

        public static ISubmissionData<Unit> Remove(string name) =>
            Do(data => data.Remove(name));

        /// <summary>
        /// Sets the value of a field identified by its name.
        /// </summary>

        public static ISubmissionData<Unit> Set(string name, string value) =>
            Do(data => data[name] = value);

        /// <summary>
        /// Sets the values of all fields identified by a collection of
        /// names to the same value.
        /// </summary>

        public static ISubmissionData<Unit> Set(IEnumerable<string> names, string value) =>
            from _ in For(names, n => Set(n, value))
            select Unit.Default;

        static ISubmissionDataAction<string> TrySet(Func<IEnumerable<string>, string> matcher, string value) =>
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

        public static ISubmissionDataAction<string> SetSingleWhere(Func<string, bool> matcher, string value) =>
            TrySet(ns => ns.Single(matcher), value);

        /// <summary>
        /// Sets the value of a single field identified by a regular expression
        /// pattern otherwise throws an error.
        /// </summary>
        /// <returns>
        /// The name of the field whose value was set.
        /// </returns>

        public static ISubmissionDataAction<string> SetSingleMatching(string pattern, string value) =>
            SetSingleWhere(n => Regex.IsMatch(n, pattern), value);

        /// <summary>
        /// Attempts to set the value of a single field identified by a
        /// predicate function otherwise has no effect.
        /// </summary>
        /// <returns>
        /// The name of the field whose value was set or <c>null</c> if zero
        /// or multiple fields were identified.
        /// </returns>

        public static ISubmissionDataAction<string> TrySetSingleWhere(Func<string, bool> matcher, string value) =>
            TrySet(ns => ns.SingleOrDefault(matcher), value);

        /// <summary>
        /// Attempts to Set the value of a single field identified by a
        /// regular expression pattern otherwise has no effect.
        /// </summary>
        /// <returns>
        /// The name of the field whose value was set or <c>null</c> if zero
        /// or multiple fields were identified.
        /// </returns>

        public static ISubmissionDataAction<string> TrySetSingleMatching(string pattern, string value) =>
            TrySetSingleWhere(n => Regex.IsMatch(n, pattern), value);

        /// <summary>
        /// Sets the value of the first field identified by a predicate
        /// function otherwise throws an error if no field was identified.
        /// </summary>
        /// <returns>
        /// The name of the first field identified by the predicate function.
        /// </returns>

        public static ISubmissionDataAction<string> SetFirstWhere(Func<string, bool> matcher, string value) =>
            TrySet(ns => ns.First(matcher), value);

        /// <summary>
        /// Attempts to set the value of the first field identified by a
        /// predicate function otherwise has no effect if no field was
        /// identified.
        /// </summary>
        /// <returns>
        /// The name of the first field identified by the predicate function.
        /// </returns>

        public static ISubmissionDataAction<string> TrySetFirstWhere(Func<string, bool> matcher, string value) =>
            TrySet(ns => ns.FirstOrDefault(matcher), value);

        /// <summary>
        /// Attempts to set the value of the first field identified by a
        /// regular expression pattern otherwise has no effect if no field was
        /// identified.
        /// </summary>
        /// <returns>
        /// The name of the first field identified by the predicate function.
        /// </returns>

        public static ISubmissionDataAction<string> TrySetFirstMatching(string pattern, string value) =>
            TrySetFirstWhere(n => Regex.IsMatch(n, pattern), value);

        /// <summary>
        /// Sets the values of all fields identified by a predicate function
        /// to the same value.
        /// </summary>
        /// <returns>
        /// A sequence of field names that were identified by the predicate
        /// function and affected.
        /// </returns>

        public static ISubmissionDataAction<IEnumerable<string>> SetWhere(Func<string, bool> matcher, string value) =>
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

        public static ISubmissionDataAction<IEnumerable<string>> SetMatching(string pattern, string value) =>
            SetWhere(n => Regex.IsMatch(n, pattern), value);

        public static ISubmissionData<Unit> Merge(NameValueCollection other) =>
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

        public static ISubmissionData<NameValueCollection> Data() =>
            Create(data => new NameValueCollection(data));

        /// <summary>
        /// Clears all form data.
        /// </summary>

        public static ISubmissionData<Unit> Clear() =>
            Do(data => data.Clear());

        /// <summary>
        /// Changes the type of the submission to <seealso cref="Unit"/>.
        /// </summary>

        public static ISubmissionData<Unit> Ignore<T>(this ISubmissionData<T> submission) =>
            from _ in submission
            select Unit.Default;

        /// <summary>
        /// Continues one submission after another.
        /// </summary>

        public static ISubmissionData<T> Then<T>(this ISubmissionData<Unit> first, ISubmissionData<T> second) =>
            from _ in first
            from b in second
            select b;

        /// <summary>
        /// Combines the result of one submission with another.
        /// </summary>

        public static ISubmissionData<TResult>
            Zip<TFirst, TSecond, TResult>(
                this ISubmissionData<TFirst> first,
                ISubmissionData<TSecond> second,
                Func<TFirst, TSecond, TResult> resultSelector) =>
            from a in first
            from b in second
            select resultSelector(a, b);

        public static ISubmissionData<Unit> Collect(params ISubmissionData<Unit>[] submissions) =>
            submissions.AsEnumerable().Collect();

        public static ISubmissionData<Unit> Collect(this IEnumerable<ISubmissionData<Unit>> submissions) =>
            For(submissions, s => s).Ignore();
    }
}

namespace WebLinq.Html
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using static SubmissionData;

    public static class FormSubmissionData
    {
        static ISubmissionDataAction<string> TrySet(this HtmlForm form, Func<IEnumerable<string>, string> matcher, string value) =>
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

        public static ISubmissionDataAction<string> SetSingleWhere(this HtmlForm form, Func<string, bool> matcher, string value) =>
            form.TrySet(ns => ns.Single(matcher), value);

        /// <summary>
        /// Sets the value of a single field identified by a regular expression
        /// pattern otherwise throws an error.
        /// </summary>
        /// <returns>
        /// The name of the field whose value was set.
        /// </returns>

        public static ISubmissionDataAction<string> SetSingleMatching(this HtmlForm form, string pattern, string value) =>
            form.SetSingleWhere(n => Regex.IsMatch(n, pattern), value);

        /// <summary>
        /// Attempts to set the value of a single field identified by a
        /// predicate function otherwise has no effect.
        /// </summary>
        /// <returns>
        /// The name of the field whose value was set or <c>null</c> if zero
        /// or multiple fields were identified.
        /// </returns>

        public static ISubmissionDataAction<string> TrySetSingleWhere(this HtmlForm form, Func<string, bool> matcher, string value) =>
            form.TrySet(ns => ns.SingleOrDefault(matcher), value);

        /// <summary>
        /// Attempts to Set the value of a single field identified by a
        /// regular expression pattern otherwise has no effect.
        /// </summary>
        /// <returns>
        /// The name of the field whose value was set or <c>null</c> if zero
        /// or multiple fields were identified.
        /// </returns>

        public static ISubmissionDataAction<string> TrySetSingleMatching(this HtmlForm form, string pattern, string value) =>
            form.TrySetSingleWhere(n => Regex.IsMatch(n, pattern), value);

        /// <summary>
        /// Sets the value of the first field identified by a predicate
        /// function otherwise throws an error if no field was identified.
        /// </summary>
        /// <returns>
        /// The name of the first field identified by the predicate function.
        /// </returns>

        public static ISubmissionDataAction<string> SetFirstWhere(this HtmlForm form, Func<string, bool> matcher, string value) =>
            form.TrySet(ns => ns.First(matcher), value);

        /// <summary>
        /// Attempts to set the value of the first field identified by a
        /// predicate function otherwise has no effect if no field was
        /// identified.
        /// </summary>
        /// <returns>
        /// The name of the first field identified by the predicate function.
        /// </returns>

        public static ISubmissionDataAction<string> TrySetFirstWhere(this HtmlForm form, Func<string, bool> matcher, string value) =>
            form.TrySet(ns => ns.FirstOrDefault(matcher), value);

        /// <summary>
        /// Attempts to set the value of the first field identified by a
        /// regular expression pattern otherwise has no effect if no field was
        /// identified.
        /// </summary>
        /// <returns>
        /// The name of the first field identified by the predicate function.
        /// </returns>

        public static ISubmissionDataAction<string> TrySetFirstMatching(this HtmlForm form, string pattern, string value) =>
            form.TrySetFirstWhere(n => Regex.IsMatch(n, pattern), value);

        /// <summary>
        /// Sets the values of all fields identified by a predicate function
        /// to the same value.
        /// </summary>
        /// <returns>
        /// A sequence of field names that were identified by the predicate
        /// function and affected.
        /// </returns>

        public static ISubmissionDataAction<IEnumerable<string>> SetWhere(this HtmlForm form, Func<string, bool> matcher, string value) =>
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

        public static ISubmissionDataAction<IEnumerable<string>> SetMatching(this HtmlForm form, string pattern, string value) =>
            form.SetWhere(n => Regex.IsMatch(n, pattern), value);
    }
}
