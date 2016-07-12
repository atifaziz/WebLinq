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
    using System.Collections.Specialized;
    using System.Linq;
    using System.Net;
    using System.Net.Mime;

    #endregion

    public sealed class HtmlForm
    {
        ICollection<HtmlFormControl> _controls;

        public HtmlObject Element { get; }
        public string Name { get; }
        public string Action { get; }
        public HtmlFormMethod Method { get; }
        public ContentType EncType { get; }

        public ICollection<HtmlFormControl> Controls =>
            _controls ?? (_controls = Array.AsReadOnly(GetControlsCore(Element).ToArray()));

        internal HtmlForm(HtmlObject element, string name, string action, HtmlFormMethod method, ContentType encType)
        {
            Element  = element;
            Name     = name;
            Action   = action;
            Method   = method;
            EncType  = encType;
        }

        public override string ToString() => Element.ToString();

        static IEnumerable<HtmlFormControl> GetControlsCore(HtmlObject formElement)
        {
            //
            // Grab all INPUT and SELECT elements belonging to the form.
            //
            // TODO: BUTTON
            // TODO: formaction https://developer.mozilla.org/en-US/docs/Web/HTML/Element/button#attr-formaction
            // TODO: formenctype https://developer.mozilla.org/en-US/docs/Web/HTML/Element/button#attr-formenctype
            // TODO: formmethod https://developer.mozilla.org/en-US/docs/Web/HTML/Element/button#attr-formmethod
            //

            const string @readonly = "readonly";
            const string disabled = "disabled";

            return
                from e in formElement.QuerySelectorAll("input, select, textarea")
                let name = e.GetAttributeValue("name")?.Trim() ?? string.Empty
                where name.Length > 0
                let controlType = "select".Equals(e.Name, StringComparison.OrdinalIgnoreCase)
                                ? HtmlControlType.Select
                                : "textarea".Equals(e.Name, StringComparison.OrdinalIgnoreCase)
                                ? HtmlControlType.TextArea
                                : HtmlControlType.Input
                let attrs = new
                {
                    Disabled  = e.GetAttributeValue(disabled)?.Trim(),
                    ReadOnly  = e.GetAttributeValue(@readonly)?.Trim(),
                    InputType = controlType == HtmlControlType.Input
                                ? e.GetAttributeValue("type")?.Trim().Map(HtmlInputType.Parse)
                                // Missing "type" attribute implies "text" since HTML 3.2
                                ?? HtmlInputType.Default
                                : null,
                }
                select new HtmlFormControl
                (
                    e,
                    name,
                    controlType,
                    attrs.InputType,
                    disabled.Equals(attrs.Disabled, StringComparison.OrdinalIgnoreCase) ? HtmlDisabledFlag.Disabled : HtmlDisabledFlag.Default,
                    @readonly.Equals(attrs.ReadOnly, StringComparison.OrdinalIgnoreCase) ? HtmlReadOnlyFlag.ReadOnly : HtmlReadOnlyFlag.Default
                );
        }

        public NameValueCollection GetForm() =>
            GetFormCore(data => data);

        public T GetForm<T>(Func<NameValueCollection, NameValueCollection, T> selector) =>
            GetFormCore(selector2: selector);

        public T GetForm<T>(Func<NameValueCollection, NameValueCollection, NameValueCollection, T> selector) =>
            GetFormCore(selector3: selector);

        T GetFormCore<T>(Func<NameValueCollection, T> selector1 = null,
                         Func<NameValueCollection, NameValueCollection, T> selector2 = null,
                         Func<NameValueCollection, NameValueCollection, NameValueCollection, T> selector3 = null)
        {
            // TODO Validate formElement is FORM
            // TODO formmethod, formaction, formenctype

            var all          = selector3 != null ? new NameValueCollection() : null;
            var form         = new NameValueCollection();
            var submittables = selector1 == null ? new NameValueCollection() : null;

            //
            // Controls are collected into one or more of following buckets:
            //
            // - all           (including disabled ones)
            // - form          (enabled, non-submittables)
            // - submittables  (just the enabled submittables)
            //
            // See section 4.10.19.6[1] (Form submission) in HTML5
            // specification as well as section 17.13[2] (Form submission) in
            // the older HTML 4.01 Specification for details.
            //
            // [1]: https://www.w3.org/TR/html5/forms.html#form-submission
            // [2]: http://www.w3.org/TR/html401/interact/forms.html#h-17.13

            foreach (var field in
                from c in Controls
                select new
                {
                    c.Element,
                    c.Name,
                    IsSelect   = c.ControlType == HtmlControlType.Select,
                    c.InputType,
                    IsDisabled = c.Disabled != HtmlDisabledFlag.Default,
                    IsReadOnly = c.ReadOnly != HtmlReadOnlyFlag.Default,
                })
            {
                if (!field.IsSelect && field.InputType.KnownType == KnownHtmlInputType.Other)
                    throw new Exception($"Unexpected type of form field (\"{field.Name}\").");

                var valueElement = field.IsSelect
                                 ? field.Element.QuerySelector("option[selected]")
                                 : field.Element;

                var value = valueElement?.GetAttributeValue("value") ?? string.Empty;

                all?.Add(field.Name, value);

                if (field.IsDisabled)
                    continue;

                var bucket = field.InputType == HtmlInputType.Submit
                             || field.InputType == HtmlInputType.Button
                             || field.InputType == HtmlInputType.Image
                           ? submittables
                           : field.InputType != HtmlInputType.Reset
                             && field.InputType != HtmlInputType.File
                           ? form
                           : null;

                bucket?.Add(field.Name, value);
            }

            return selector3 != null ? selector3(all, form, submittables)
                 : selector2 != null ? selector2(form, submittables)
                 : selector1 != null ? selector1(form)
                 : default(T);
        }
    }
}