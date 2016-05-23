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

namespace WebLinq
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Mime;
    using Fizzler.Systems.HtmlAgilityPack;
    using HtmlAgilityPack;
    using TryParsers;

    public interface IHtmlParser
    {
        IParsedHtml Parse(string html);
    }

    public interface IParsedHtml
    {
        string OuterHtml(string selector);
        IEnumerable<T> Links<T>(Uri baseUrl, Func<string, string, T> selector);
        IEnumerable<string> Tables(string selector);
        IEnumerable<T> Forms<T>(string cssSelector, Func<string, string, string, HtmlFormMethod, ContentType, string, T> selector);
    }

    public enum HtmlFormMethod { Get, Post }

    public sealed class HtmlParser : IHtmlParser
    {
        readonly QueryContext _context;

        public HtmlParser(QueryContext context)
        {
            _context = context;
        }

        public IParsedHtml Parse(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml2(html);
            return new ParsedHtml(doc);
        }

        sealed class ParsedHtml : IParsedHtml
        {
            readonly HtmlDocument _document;
            readonly Lazy<Uri> _cachedBaseUrl;

            static class Selectors
            {
                public static readonly Func<HtmlNode, IEnumerable<HtmlNode>> DocBase = HtmlNodeSelection.CachableCompile("html > head > base[href]");
                public static readonly Func<HtmlNode, IEnumerable<HtmlNode>> Anchor = HtmlNodeSelection.CachableCompile("a[href]");
                public static readonly Func<HtmlNode, IEnumerable<HtmlNode>> Table = HtmlNodeSelection.CachableCompile("table");
            }

            public ParsedHtml(HtmlDocument document)
            {
                _document = document;
                _cachedBaseUrl = new Lazy<Uri>(TryGetBaseUrl);
            }

            HtmlNode DocumentNode => _document.DocumentNode;

            public string OuterHtml(string selector) =>
                DocumentNode.QuerySelector(selector)?.OuterHtml;

            public IEnumerable<T> Links<T>(Uri baseUrl, Func<string, string, T> selector)
            {
                return
                    from a in Selectors.Anchor(DocumentNode)
                    let href = a.GetAttributeValue("href", null)
                    where !string.IsNullOrWhiteSpace(href)
                    select selector(Href(baseUrl, href), a.InnerHtml);
            }

            public IEnumerable<string> Tables(string selector)
            {
                var sel = selector == null
                        ? Selectors.Table
                        : HtmlNodeSelection.CachableCompile(selector);
                return
                    from e in sel(DocumentNode)
                    where "table".Equals(e.Name, StringComparison.OrdinalIgnoreCase)
                    select e.OuterHtml;
            }

            string Href(Uri baseUrl, string href) =>
                HrefImpl(baseUrl ?? CachedBaseUrl, href);

            static string HrefImpl(Uri baseUrl, string href)
            {
                return baseUrl != null
                     ? TryParse.Uri(baseUrl, href)?.OriginalString ?? href
                     : href;
            }

            Uri CachedBaseUrl => _cachedBaseUrl.Value;

            Uri TryGetBaseUrl()
            {
                var baseRef = Selectors.DocBase(DocumentNode)
                                       .FirstOrDefault()
                                      ?.GetAttributeValue("href", null);

                if (baseRef == null)
                    return null;

                var baseUrl = TryParse.Uri(baseRef, UriKind.Absolute);

                return baseUrl.Scheme == Uri.UriSchemeHttp || baseUrl.Scheme == Uri.UriSchemeHttps
                     ? baseUrl : null;
            }

            public IEnumerable<T> Forms<T>(string cssSelector, Func<string, string, string, HtmlFormMethod, ContentType, string, T> selector) =>
                from form in DocumentNode.QuerySelectorAll(cssSelector ?? "form[action]")
                where "form".Equals(form.Name, StringComparison.OrdinalIgnoreCase)
                let method = form.GetAttributeValue("method", null)?.Trim()
                let enctype = form.GetAttributeValue("enctype", null)?.Trim()
                select selector(form.GetAttributeValue("id", null),
                                form.GetAttributeValue("name", null),
                                form.GetAttributeValue("action", null),
                                "post".Equals(method, StringComparison.OrdinalIgnoreCase)
                                    ? HtmlFormMethod.Post
                                    : HtmlFormMethod.Get,
                                enctype != null ? new ContentType(enctype) : null,
                                form.OuterHtml);

            public override string ToString() =>
                DocumentNode.OuterHtml;
        }
    }
}