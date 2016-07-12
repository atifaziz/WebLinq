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

namespace WebLinq.Html
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;
    using System.Net.Mime;
    using TryParsers;

    public abstract class ParsedHtml
    {
        readonly Uri _baseUrl;
        readonly Lazy<Uri> _inlineBaseUrl;
        ReadOnlyCollection<HtmlForm> _forms;

        protected ParsedHtml() :
            this(null) {}

        protected ParsedHtml(Uri baseUrl)
        {
            _baseUrl = baseUrl;
            _inlineBaseUrl = new Lazy<Uri>(TryGetInlineBaseUrl);
        }

        public Uri BaseUrl => _baseUrl ?? _inlineBaseUrl.Value;

        Uri TryGetInlineBaseUrl()
        {
            var baseRef = QuerySelector("html > head > base[href]")?.GetAttributeValue("href");

            if (baseRef == null)
                return null;

            var baseUrl = TryParse.Uri(baseRef, UriKind.Absolute);

            return baseUrl.Scheme == Uri.UriSchemeHttp || baseUrl.Scheme == Uri.UriSchemeHttps
                 ? baseUrl : null;
        }

        public IEnumerable<HtmlObject> QuerySelectorAll(string selector) =>
            QuerySelectorAll(selector, null);

        public abstract IEnumerable<HtmlObject> QuerySelectorAll(string selector, HtmlObject context);

        public HtmlObject QuerySelector(string selector) =>
            QuerySelector(selector, null);

        public virtual HtmlObject QuerySelector(string selector, HtmlObject context) =>
            QuerySelectorAll(selector, context).FirstOrDefault();

        public abstract HtmlObject Root { get; }

        public string TryBaseHref(string href) =>
            BaseUrl != null
            ? TryParse.Uri(BaseUrl, href)?.OriginalString ?? href
            : href;

        public override string ToString() => Root?.OuterHtml ?? string.Empty;

        public IReadOnlyList<HtmlForm> Forms => _forms ?? (_forms = Array.AsReadOnly(GetFormsCore().ToArray()));

        IEnumerable <HtmlForm> GetFormsCore() =>
            from form in QuerySelectorAll("form[action]")
            where "form".Equals(form.Name, StringComparison.OrdinalIgnoreCase)
            let method = form.GetAttributeValue("method")?.Trim()
            let enctype = form.GetAttributeValue("enctype")?.Trim()
            let action = form.GetAttributeValue("action")
            select new HtmlForm(form,
                                form.GetAttributeValue("name"),
                                action != null ? form.Owner.TryBaseHref(action) : action,
                                "post".Equals(method, StringComparison.OrdinalIgnoreCase)
                                    ? HtmlFormMethod.Post
                                    : HtmlFormMethod.Get,
                                enctype != null ? new ContentType(enctype) : null);

        public IEnumerable<HtmlForm> QueryFormSelectorAll(string selector) =>
            from e in QuerySelectorAll(selector ?? "form[action]")
            where selector == null || e.IsNamed("form")
            select Forms.FirstOrDefault(f => f.Element == e) into f
            where f != null
            select f;
    }

    public enum HtmlFormMethod { Get, Post }
    public enum HtmlControlType { Input, Select, TextArea }

    public static class ParsedHtmlExtensions
    {
        public static IEnumerable<T> Links<T>(this ParsedHtml self, Func<string, HtmlObject, T> selector)
        {
            return
                from a in self.QuerySelectorAll("a[href]")
                let href = a.GetAttributeValue("href")
                where !string.IsNullOrWhiteSpace(href)
                select selector(self.TryBaseHref(href), a);
        }

        public static IEnumerable<HtmlObject> Tables(this ParsedHtml self, string selector) =>
            from e in self.QuerySelectorAll(selector ?? "table")
            where "table".Equals(e.Name, StringComparison.OrdinalIgnoreCase)
            select e;



        public static IEnumerable<T> TableRows<T>(this HtmlObject table, Func<HtmlObject, IEnumerable<HtmlObject>, T> rowSelector)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            if (!table.IsNamed("table"))
                throw new ArgumentException($"Element is <{table.Name}> and not an HTML table.", nameof(table));
            if (rowSelector == null) throw new ArgumentNullException(nameof(rowSelector));
            return TableRowsCore(table, rowSelector);
        }

        static IEnumerable<T> TableRowsCore<T>(HtmlObject table, Func<HtmlObject, IEnumerable<HtmlObject>, T> rowSelector)
        {
            // TODO https://www.w3.org/TR/html5/tabular-data.html#processing-model-1

            var spans = new ArrayList<CellSpan>();

            foreach (var e in
                from g in table.ChildElements
                where g.IsNamed("tbody") || g.IsNamed("thead") || g.IsNamed("tfoot") || g.IsNamed("tr")
                from tr in g.IsNamed("tr")
                         ? new[] { g } :
                         g.ChildElements.Where(e => e.IsNamed("tr"))
                select new
                {
                    Row   = tr,
                    Cells = tr.ChildElements.Where(e => e.IsNamed("td") || e.IsNamed("th")).ToArray(),
                })
            {
                var tds = e.Cells;
                spans.EnsureCapacity(tds.Length);

                var cells = new ArrayList<HtmlObject>();
                cells.EnsureCapacity(tds.Length);

                var i = 0;
                foreach (var td in tds)
                {
                    while (true)
                    {
                        var rspan = spans[i, CellSpan.Zero];
                        if (rspan.Rows == 0)
                            break;
                        i += rspan.Cols;
                    }

                    var colspan = TryParse.Int32(td.GetAttributeValue("colspan"), NumberStyles.Integer & ~NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture) ?? 1;
                    var rowspan = TryParse.Int32(td.GetAttributeValue("rowspan"), NumberStyles.Integer & ~NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture) ?? 1;
                    var span = colspan > 1 || rowspan > 1 ? new CellSpan(colspan, rowspan) : CellSpan.One;

                    spans[i] = span;
                    cells[i] = td;

                    i += span.Cols;
                }

                for (i = 0; i < spans.Count; i++)
                {
                    var span = spans[i];
                    var rowspan = span.Rows - 1;
                    spans[i] = rowspan > 0 ? new CellSpan(span.Cols, rowspan) : CellSpan.Zero;
                }

                yield return rowSelector(e.Row, cells.ToArray());
            }
        }

        struct CellSpan : IEquatable<CellSpan>
        {
            public static readonly CellSpan Zero = new CellSpan(0, 0);
            public static readonly CellSpan One = new CellSpan(1, 1);

            public readonly int Cols;
            public readonly int Rows;

            public CellSpan(int cols, int rows) { Rows = rows; Cols = cols; }
            public bool Equals(CellSpan other) => Cols == other.Cols && Rows == other.Rows;
            public override bool Equals(object obj) => obj is CellSpan && Equals((CellSpan)obj);
            public override int GetHashCode() => unchecked((Cols * 397) ^ Rows);
            public override string ToString() => $"[{Cols}, {Rows}]";
        }
    }
}
