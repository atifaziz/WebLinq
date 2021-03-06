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

    public interface IHtmlParser
    {
        ParsedHtml Parse(string html, Uri baseUrl);
    }

    public static class HtmlParser
    {
        public static IHtmlParser Default => new HapHtmlParser();

        public static ParsedHtml Parse(this IHtmlParser parser, string html) =>
            parser.Parse(html, null);

        public static IHtmlParser Wrap(this IHtmlParser parser, Func<IHtmlParser, string, Uri, ParsedHtml> impl) =>
            new DelegatingHtmlParser((html, baseUrl) => impl(parser, html, baseUrl));

        sealed class DelegatingHtmlParser : IHtmlParser
        {
            readonly Func<string, Uri, ParsedHtml> _parser;

            public DelegatingHtmlParser(Func<string, Uri, ParsedHtml> parser)
            {
                _parser = parser;
            }

            public ParsedHtml Parse(string html, Uri baseUrl) => _parser(html, baseUrl);
        }
    }
}
