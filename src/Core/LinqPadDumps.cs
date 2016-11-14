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

// The methods in this file make WebLinq types friendlier to view in
// LINQPad. See http://www.linqpad.net/CustomizingDump.aspx for more.

namespace WebLinq
{
    using System;
    using System.Linq;

    partial class Query<T>
    {
        internal object ToDump() =>
            new ObservableQuery(this);

        sealed class ObservableQuery : IObservable<T>
        {
            readonly IEnumerable<QueryContext, T> _query;

            public ObservableQuery(IEnumerable<QueryContext, T> query)
            {
                _query = query;
            }

            public IDisposable Subscribe(IObserver<T> observer)
            {
                Exception error = null;
                try
                {
                    foreach (var e in _query.ToEnumerable(DefaultQueryContext.Create))
                        observer.OnNext(e);
                }
                catch (Exception e)
                {
                    error = e;
                }

                if (error != null) observer.OnError(error);
                else observer.OnCompleted();

                return NopDisposable.Instance;
            }

            sealed class NopDisposable : IDisposable
            {
                public static readonly NopDisposable Instance = new NopDisposable();
                NopDisposable() {}
                public void Dispose() {}
            }
        }
    }

    partial class HttpFetch<T>
    {
        internal object ToDump() => new
        {
            Id,
            HttpVersion = HttpVersion.ToString(),
            StatusCode,
            RequestUrl,

            Headers =
                from e in Headers
                select new
                {
                    e.Key,
                    Value = e.Value.Count == 1
                          ? (object) e.Value.Single()
                          : e.Value
                },

            Content,
        };
    }
}

namespace WebLinq.Html
{
    using System;
    using System.Linq;

    partial class HtmlObject
    {
        internal object ToDump() => new
        {
            Name,

            Attributes =
                from an in AttributeNames
                select new { Name = an, Value = GetAttributeValue(an) },

            ChildElementCount =
                HasChildElements ? ChildElements.Count() : 0,

            OuterHtml =
                OuterHtml.Length <= 300
                ? (object)OuterHtml
                : new Lazy<string>(() => OuterHtml)
        };
    }

    partial class HtmlForm
    {
        internal object ToDump() => new
        {
            Name,
            Action,
            Method,
            EncType,
            Controls,
            Element = new Lazy<HtmlObject>(() => Element),
        };
    }

    partial class HtmlFormControl
    {
        internal object ToDump() => new
        {
            Name,
            ControlType,
            InputType = InputType?.KnownType,
            IsDisabled,
            IsReadOnly,
            IsChecked,
            IsMultiple,
            Element = new Lazy<HtmlObject>(() => Element),
            Form = new Lazy<HtmlForm>(() => Form),
        };
    }
}
