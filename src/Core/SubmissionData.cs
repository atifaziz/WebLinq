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

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using System;

namespace WebLinq
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;

    public interface ISubmissionData<out T>
    {
        T Run(NameValueCollection data);
    }

    public interface ISubmissionDataAction<out T> : ISubmissionData<Unit>
    {
#pragma warning disable CA1716 // Identifiers should not match keywords (by-design)
        ISubmissionData<T> Return();
#pragma warning restore CA1716 // Identifiers should not match keywords
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
            Create(env => { action(env); return new Unit(); });
    }
}

namespace WebLinq
{
    public static partial class SubmissionData
    {
        /// <summary>
        /// Represents nothing.
        /// </summary>

        public static readonly ISubmissionData<Unit> None = Do(delegate { });
    }
}
