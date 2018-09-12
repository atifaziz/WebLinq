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

    public interface IFormSubmission<out T>
    {
        T Run(FormSubmissionContext context);
    }

    public static partial class FormSubmission
    {
        public static IFormSubmission<T> Create<T>(Func<FormSubmissionContext, T> runner) =>
            new DelegatingFormSubmission<T>(runner);

        sealed class DelegatingFormSubmission<T> : IFormSubmission<T>
        {
            readonly Func<FormSubmissionContext, T> _runner;

            public DelegatingFormSubmission(Func<FormSubmissionContext, T> runner) =>
                _runner = runner ?? throw new ArgumentNullException(nameof(runner));

            public T Run(FormSubmissionContext context) =>
                _runner(context ?? throw new ArgumentNullException(nameof(context)));
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
            Create(context => source.Select(f).Select(e => e.Run(context)).ToList());

        internal static IFormSubmission<T> Do<T>(this IFormSubmission<T> submission, Action<FormSubmissionContext> action) =>
            submission.Bind(x => Create(env => { action(env); return x; }));

        internal static IFormSubmission<Unit> Do(Action<FormSubmissionContext> action) =>
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
            Create(context => context.Data.AllKeys);

        /// <summary>
        /// Gets the parsed underlying HTML form.
        /// </summary>

        public static IFormSubmission<HtmlForm> Form() =>
            Create(context => context.Form);

        /// <summary>
        /// Gets the value of a field identified by its name.
        /// </summary>

        public static IFormSubmission<string> Get(string name) =>
            Create(context => context.Data[name]);

        /// <summary>
        /// Gets all the values of a field identified by its name.
        /// </summary>

        public static IFormSubmission<IReadOnlyCollection<string>> GetValues(string name) =>
            Create(context => context.Data.GetValues(name));

        /// <summary>
        /// Removes a field from submission.
        /// </summary>

        public static IFormSubmission<Unit> Remove(string name) =>
            Do(context => context.Data.Remove(name));

        /// <summary>
        /// Sets the value of a field identified by its name.
        /// </summary>

        public static IFormSubmission<Unit> Set(string name, string value) =>
            Do(context => context.Data[name] = value);

        /// <summary>
        /// Sets the values of all fields identified by a collection of
        /// names to the same value.
        /// </summary>

        public static IFormSubmission<Unit> Set(IEnumerable<string> names, string value) =>
            from _ in For(names, n => Set(n, value))
            select Unit.Default;

        static IFormSubmission<string> TrySet(Func<IEnumerable<string>, string> matcher, string value) =>
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

        public static IFormSubmission<string> SetSingleWhere(Func<string, bool> matcher, string value) =>
            TrySet(ns => ns.Single(matcher), value);

        /// <summary>
        /// Sets the value of a single field identified by a regular expression
        /// pattern otherwise throws an error.
        /// </summary>
        /// <returns>
        /// The name of the field whose value was set.
        /// </returns>

        public static IFormSubmission<string> SetSingleMatching(string pattern, string value) =>
            SetSingleWhere(n => Regex.IsMatch(n, pattern), value);

        /// <summary>
        /// Attempts to set the value of a single field identified by a
        /// predicate function otherwise has no effect.
        /// </summary>
        /// <returns>
        /// The name of the field whose value was set or <c>null</c> if zero
        /// or multiple fields were identified.
        /// </returns>

        public static IFormSubmission<string> TrySetSingleWhere(Func<string, bool> matcher, string value) =>
            TrySet(ns => ns.SingleOrDefault(matcher), value);

        /// <summary>
        /// Attempts to Set the value of a single field identified by a
        /// regular expression pattern otherwise has no effect.
        /// </summary>
        /// <returns>
        /// The name of the field whose value was set or <c>null</c> if zero
        /// or multiple fields were identified.
        /// </returns>

        public static IFormSubmission<string> TrySetSingleMatching(string pattern, string value) =>
            TrySetSingleWhere(n => Regex.IsMatch(n, pattern), value);

        /// <summary>
        /// Sets the value of the first field identified by a predicate
        /// function otherwise throws an error if no field was identified.
        /// </summary>
        /// <returns>
        /// The name of the first field identified by the predicate function.
        /// </returns>

        public static IFormSubmission<string> SetFirstWhere(Func<string, bool> matcher, string value) =>
            TrySet(ns => ns.First(matcher), value);

        /// <summary>
        /// Attempts to set the value of the first field identified by a
        /// predicate function otherwise has no effect if no field was
        /// identified.
        /// </summary>
        /// <returns>
        /// The name of the first field identified by the predicate function.
        /// </returns>

        public static IFormSubmission<string> TrySetFirstWhere(Func<string, bool> matcher, string value) =>
            TrySet(ns => ns.FirstOrDefault(matcher), value);

        /// <summary>
        /// Attempts to set the value of the first field identified by a
        /// regular expression pattern otherwise has no effect if no field was
        /// identified.
        /// </summary>
        /// <returns>
        /// The name of the first field identified by the predicate function.
        /// </returns>

        public static IFormSubmission<string> TrySetFirstMatching(string pattern, string value) =>
            TrySetFirstWhere(n => Regex.IsMatch(n, pattern), value);

        /// <summary>
        /// Sets the values of all fields identified by a predicate function
        /// to the same value.
        /// </summary>
        /// <returns>
        /// A sequence of field names that were identified by the predicate
        /// function and affected.
        /// </returns>

        public static IFormSubmission<IEnumerable<string>> SetWhere(Func<string, bool> matcher, string value) =>
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

        public static IFormSubmission<IEnumerable<string>> SetMatching(string pattern, string value) =>
            SetWhere(n => Regex.IsMatch(n, pattern), value);

        public static IFormSubmission<Unit> Merge(NameValueCollection other) =>
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

        public static IFormSubmission<NameValueCollection> Data() =>
            Create(context => new NameValueCollection(context.Data));

        /// <summary>
        /// Clears all form data.
        /// </summary>

        public static IFormSubmission<Unit> Clear() =>
            Do(context => context.Data.Clear());

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
        static IFormSubmission<string> TrySet(this HtmlForm form, Func<IEnumerable<string>, string> matcher, string value) =>
            from ns in Return(from c in form.Controls select c.Name)
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

        public static IFormSubmission<string> SetSingleWhere(this HtmlForm form, Func<string, bool> matcher, string value) =>
            form.TrySet(ns => ns.Single(matcher), value);

        /// <summary>
        /// Sets the value of a single field identified by a regular expression
        /// pattern otherwise throws an error.
        /// </summary>
        /// <returns>
        /// The name of the field whose value was set.
        /// </returns>

        public static IFormSubmission<string> SetSingleMatching(this HtmlForm form, string pattern, string value) =>
            form.SetSingleWhere(n => Regex.IsMatch(n, pattern), value);

        /// <summary>
        /// Attempts to set the value of a single field identified by a
        /// predicate function otherwise has no effect.
        /// </summary>
        /// <returns>
        /// The name of the field whose value was set or <c>null</c> if zero
        /// or multiple fields were identified.
        /// </returns>

        public static IFormSubmission<string> TrySetSingleWhere(this HtmlForm form, Func<string, bool> matcher, string value) =>
            form.TrySet(ns => ns.SingleOrDefault(matcher), value);

        /// <summary>
        /// Attempts to Set the value of a single field identified by a
        /// regular expression pattern otherwise has no effect.
        /// </summary>
        /// <returns>
        /// The name of the field whose value was set or <c>null</c> if zero
        /// or multiple fields were identified.
        /// </returns>

        public static IFormSubmission<string> TrySetSingleMatching(this HtmlForm form, string pattern, string value) =>
            form.TrySetSingleWhere(n => Regex.IsMatch(n, pattern), value);

        /// <summary>
        /// Sets the value of the first field identified by a predicate
        /// function otherwise throws an error if no field was identified.
        /// </summary>
        /// <returns>
        /// The name of the first field identified by the predicate function.
        /// </returns>

        public static IFormSubmission<string> SetFirstWhere(this HtmlForm form, Func<string, bool> matcher, string value) =>
            form.TrySet(ns => ns.First(matcher), value);

        /// <summary>
        /// Attempts to set the value of the first field identified by a
        /// predicate function otherwise has no effect if no field was
        /// identified.
        /// </summary>
        /// <returns>
        /// The name of the first field identified by the predicate function.
        /// </returns>

        public static IFormSubmission<string> TrySetFirstWhere(this HtmlForm form, Func<string, bool> matcher, string value) =>
            form.TrySet(ns => ns.FirstOrDefault(matcher), value);

        /// <summary>
        /// Attempts to set the value of the first field identified by a
        /// regular expression pattern otherwise has no effect if no field was
        /// identified.
        /// </summary>
        /// <returns>
        /// The name of the first field identified by the predicate function.
        /// </returns>

        public static IFormSubmission<string> TrySetFirstMatching(this HtmlForm form, string pattern, string value) =>
            form.TrySetFirstWhere(n => Regex.IsMatch(n, pattern), value);

        /// <summary>
        /// Sets the values of all fields identified by a predicate function
        /// to the same value.
        /// </summary>
        /// <returns>
        /// A sequence of field names that were identified by the predicate
        /// function and affected.
        /// </returns>

        public static IFormSubmission<IEnumerable<string>> SetWhere(this HtmlForm form, Func<string, bool> matcher, string value) =>
            from ns in Return(from c in form.Controls select c.Name)
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

        public static IFormSubmission<IEnumerable<string>> SetMatching(this HtmlForm form, string pattern, string value) =>
            form.SetWhere(n => Regex.IsMatch(n, pattern), value);
    }
}
