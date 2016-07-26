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
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using Fizzler.Systems.HtmlAgilityPack;
    using HtmlAgilityPack;

    #endregion

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
                    _map.Add(node, obj = "option".Equals(node.Name, StringComparison.Ordinal)
                                         ? new HapOptionElement(node, this)
                                         : new HapHtmlObject(node, this));
                return obj;
            }

            public override IEnumerable<HtmlObject> QuerySelectorAll(string selector, HtmlObject context) =>
                from node in (((HapHtmlObject)context)?.Node ?? _document.DocumentNode).QuerySelectorAll(selector)
                orderby node.StreamPosition
                select GetPublicObject(node);

            public override HtmlObject Root => GetPublicObject(_document.DocumentNode);

            class HapHtmlObject : HtmlObject
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
                public override HtmlString InnerTextSource =>
                    HtmlString.FromEncoded(Node.InnerText);

                public override HtmlObject ParentElement =>
                    Node.ParentNode.NodeType == HtmlNodeType.Element
                    ? _owner.GetPublicObject(Node.ParentNode)
                    : null;

                public override IEnumerable<HtmlObject> ChildElements =>
                    from e in Node.ChildNodes
                    where e.NodeType == HtmlNodeType.Element
                    select _owner.GetPublicObject(e);
            }

            sealed class HapOptionElement : HapHtmlObject
            {
                HtmlString? _innerTextSource;

                public HapOptionElement(HtmlNode node, HapParsedHtml owner) :
                    base(node, owner) {}

                public override HtmlString InnerTextSource =>
                    (_innerTextSource ?? (_innerTextSource = GetInnerTextSourceCore())).Value;

                HtmlString GetInnerTextSourceCore()
                {
                    // Workaround for HTML Agility Pack where OPTION elements
                    // do not correctly return their inner text, especially
                    // in the presence of a closing tag.
                    // https://www.w3.org/TR/html-markup/option.html#option-tags
                    // An option element's end tag may be omitted if the option
                    // element is immediately followed by another OPTION
                    // element, or if it is immediately followed by an
                    // OPTGROUP element, or if there is no more content in the
                    // parent element.

                    var siblingElement = Node.Siblings().FirstOrDefault(s => s.NodeType == HtmlNodeType.Element);
                    if (Node.InnerText.Length == 0
                        && (siblingElement == null || "option".Equals(siblingElement.Name, StringComparison.OrdinalIgnoreCase)
                                                   || "optgroup".Equals(siblingElement.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        var tns =
                            from n in Node.Siblings().TakeWhile(n => n != siblingElement)
                            where n.NodeType == HtmlNodeType.Text
                            select n.InnerText;
                        return HtmlString.FromEncoded(string.Join(null, tns));
                    }

                    return base.InnerTextSource;
                }
            }
        }
    }

    static class HapExtensions
    {
        public static IEnumerable<HtmlNode> Siblings(this HtmlNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            for (var sibling = node.NextSibling; sibling != null; sibling = sibling.NextSibling)
                yield return sibling;
        }
    }
}