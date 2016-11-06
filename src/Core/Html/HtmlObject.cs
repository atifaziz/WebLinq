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

//[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("LINQPadQeury")]

namespace WebLinq.Html
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public abstract partial class HtmlObject
    {
        public abstract ParsedHtml Owner { get; }
        public abstract string Name { get; }
        public bool IsNamed(string name) => Name.Equals(name, StringComparison.OrdinalIgnoreCase);
        public virtual bool HasAttributes => AttributeNames.Any();
        public abstract IEnumerable<string> AttributeNames { get; }
        public abstract bool HasAttribute(string name);
        public virtual string GetAttributeValue(string name) => GetAttributeSourceValue(name).Decoded;
        public virtual bool AttributeValueEquals(string name, string value) => string.Equals(GetAttributeValue(name)?.Trim(), value, StringComparison.OrdinalIgnoreCase);
        public virtual bool IsAttributeFlagged(string name) => AttributeValueEquals(name, name);
        public abstract HtmlString GetAttributeSourceValue(string name);
        public abstract string OuterHtml { get; }
        public abstract string InnerHtml { get; }
        public virtual string InnerText => InnerTextSource.Decoded;
        public abstract HtmlString InnerTextSource { get; }
        public virtual bool HasChildElements => ChildElements.Any();
        public abstract HtmlObject ParentElement { get; }
        public abstract IEnumerable<HtmlObject> ChildElements { get; }
        public override string ToString() => OuterHtml;

        public virtual IEnumerable<HtmlObject> QuerySelectorAll(string selector) =>
            Owner.QuerySelectorAll(selector, this);

        public virtual HtmlObject QuerySelector(string selector) =>
            Owner.QuerySelector(selector, this);

        string _class;
        HashSet<string> _classes;
        HashSet<string> Classes => _classes ?? (_classes = new HashSet<string>(Class.Split(' ')));

        public string Class => _class ?? (_class = GetAttributeSourceValue("class").Decoded ?? string.Empty);
        public bool HasClass(string className) => Classes.Contains(className);
    }
}
