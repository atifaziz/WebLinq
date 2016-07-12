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
    using System.Linq;
    using System.Runtime.CompilerServices;
    using Fizzler.Systems.HtmlAgilityPack;
    using HtmlAgilityPack;

    public interface IHtmlParser
    {
        ParsedHtml Parse(string html, Uri baseUrl);
    }

    public sealed class HapHtmlParser : IHtmlParser
    {
        public void Register(Action<Type, object> registrationHandler) =>
            registrationHandler(typeof(IHtmlParser), this);

        public ParsedHtml Parse(string html, Uri baseUrl)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml2(html);
            return new HapParsedHtml(doc, baseUrl);
        }

        sealed class HapParsedHtml : ParsedHtml
        {
            readonly HtmlDocument _document;
            readonly ConditionalWeakTable<HtmlNode, HtmlObject> _map = new ConditionalWeakTable<HtmlNode, HtmlObject>();

            public HapParsedHtml(HtmlDocument document, Uri baseUrl) :
                base(baseUrl)
            {
                _document = document;
            }

            HtmlObject GetPublicObject(HtmlNode node)
            {
                HtmlObject obj;
                if (!_map.TryGetValue(node, out obj))
                    _map.Add(node, obj = new HapHtmlObject(node, this));
                return obj;
            }

            public override IEnumerable<HtmlObject> QuerySelectorAll(string selector, HtmlObject context) =>
                from node in (((HapHtmlObject)context)?.Node ?? _document.DocumentNode).QuerySelectorAll(selector)
                orderby node.StreamPosition
                select GetPublicObject(node);

            public override HtmlObject Root => GetPublicObject(_document.DocumentNode);

            sealed class HapHtmlObject : HtmlObject
            {
                readonly HapParsedHtml _owner;

                public HapHtmlObject(HtmlNode node, HapParsedHtml owner)
                {
                    Node = node;
                    _owner = owner;
                }

                public override ParsedHtml Owner => _owner;
                public HtmlNode Node { get; }
                public override string Name => Node.Name;

                public override IEnumerable<string> AttributeNames =>
                    from a in Node.Attributes select a.Name;

                public override bool HasAttribute(string name) =>
                    GetAttributeValue(name) == null;

                public override HtmlString GetAttributeSourceValue(string name) =>
                    HtmlString.FromEncoded(Node.GetAttributeValue(name, null));

                public override string OuterHtml => Node.OuterHtml;
                public override string InnerHtml => Node.InnerHtml;
                public override HtmlString InnerTextSource => HtmlString.FromEncoded(Node.InnerText);

                public override HtmlObject ParentElement =>
                    Node.ParentNode.NodeType == HtmlNodeType.Element
                    ? _owner.GetPublicObject(Node.ParentNode)
                    : null;

                public override IEnumerable<HtmlObject> ChildElements =>
                    from e in Node.ChildNodes
                    where e.NodeType == HtmlNodeType.Element
                    select _owner.GetPublicObject(e);
            }
        }
    }
}