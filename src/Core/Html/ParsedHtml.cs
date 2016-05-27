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
    using System.Collections.Specialized;
    using System.Linq;
    using System.Net.Mime;
    using TryParsers;

    public abstract class ParsedHtml
    {
        readonly Uri _baseUrl;
        readonly Lazy<Uri> _inlineBaseUrl;

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
    }

    public enum HtmlControlType { Input, Select, TextArea }
    public enum HtmlDisabledFlag { Default, Disabled }
    public enum HtmlReadOnlyFlag { Default, ReadOnly }
    public enum HtmlFormMethod { Get, Post }

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

        public static IEnumerable<string> Tables(this ParsedHtml self, string selector) =>
            from e in self.QuerySelectorAll(selector ?? "table")
            where "table".Equals(e.Name, StringComparison.OrdinalIgnoreCase)
            select e.OuterHtml;

        public static IEnumerable<T> GetForms<T>(this ParsedHtml self, string cssSelector, Func<HtmlObject, string, string, string, HtmlFormMethod, ContentType, T> selector) =>
            from form in self.QuerySelectorAll(cssSelector ?? "form[action]")
            where "form".Equals(form.Name, StringComparison.OrdinalIgnoreCase)
            let method = form.GetAttributeValue("method")?.Trim()
            let enctype = form.GetAttributeValue("enctype")?.Trim()
            let action = form.GetAttributeValue("action")
            select selector(form,
                            form.GetAttributeValue("id"),
                            form.GetAttributeValue("name"),
                            action != null ? form.Owner.TryBaseHref(action) : action,
                            "post".Equals(method, StringComparison.OrdinalIgnoreCase)
                                ? HtmlFormMethod.Post
                                : HtmlFormMethod.Get,
                            enctype != null ? new ContentType(enctype) : null);

        public static IEnumerable<TForm> FormsWithControls<TControl, TForm>(this ParsedHtml self, string cssSelector, Func<string, HtmlControlType, HtmlInputType, HtmlDisabledFlag, HtmlReadOnlyFlag, string, TControl> controlSelector, Func<string, string, string, HtmlFormMethod, ContentType, string, IEnumerable<TControl>, TForm> formSelector) =>
            self.GetForms(cssSelector, (fe, id, name, action, method, enctype) =>
                formSelector(id, name, action, method, enctype, fe.OuterHtml,
                    fe.GetFormControls((ce, cn, ct, it, cd, cro) =>
                        controlSelector(cn, ct, it, cd, cro, ce.OuterHtml))));

        public static IEnumerable<T> GetFormControls<T>(this HtmlObject formElement,
            Func<HtmlObject, string, HtmlControlType, HtmlInputType, HtmlDisabledFlag, HtmlReadOnlyFlag, T> selector)
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
                select selector
                (
                    e,
                    name,
                    controlType,
                    attrs.InputType,
                    disabled.Equals(attrs.Disabled, StringComparison.OrdinalIgnoreCase) ? HtmlDisabledFlag.Disabled : HtmlDisabledFlag.Default,
                    @readonly.Equals(attrs.ReadOnly, StringComparison.OrdinalIgnoreCase) ? HtmlReadOnlyFlag.ReadOnly : HtmlReadOnlyFlag.Default
                );
        }

        public static T GetForm<T>(this HtmlObject formElement, Func<NameValueCollection, T> selector) =>
            GetFormCore(formElement, selector);

        public static T GetForm<T>(this HtmlObject formElement, Func<NameValueCollection, NameValueCollection, T> selector) =>
            GetFormCore(formElement, selector2: selector);

        public static T GetForm<T>(this HtmlObject formElement, Func<NameValueCollection, NameValueCollection, NameValueCollection, T> selector) =>
            GetFormCore(formElement, selector3: selector);

        static T GetFormCore<T>(HtmlObject formElement,
            Func<NameValueCollection, T> selector1 = null,
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

            foreach (var field in formElement.GetFormControls((node, name, ft, input, disabled, ro) => new
            {
                Element    = node,
                Name       = name,
                IsSelect   = ft == HtmlControlType.Select,
                InputType  = input,
                IsDisabled = disabled != HtmlDisabledFlag.Default,
                IsReadOnly = ro != HtmlReadOnlyFlag.Default,
            }))
            {
                if (!field.IsSelect && field.InputType.KnownType == KnownHtmlInputType.Other)
                    throw new Exception($"Unexpected type of form field (\"{field.Name}\").");

                var valueElement = field.IsSelect
                                 ? field.Element.Owner.QuerySelector("option[selected]")
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