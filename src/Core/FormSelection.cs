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

namespace WebLinq.Modules
{
    using System;
    using System.Linq;
    using Html;

    public interface IFormSelection
    {
        HtmlForm Select(ParsedHtml document);
    }

    public static class FormSelection
    {
        public static IFormSelection BySelector(string selector) =>
            new DelegatingFormSelection(doc => doc.QueryFormSelectorAll(selector).First());

        public static IFormSelection First() => ByIndex(0);

        public static IFormSelection FirstWhere(Func<HtmlForm, bool> predicate) =>
            new DelegatingFormSelection(doc => doc.Forms.First(predicate));

        public static IFormSelection SingleWhere(Func<HtmlForm, bool> predicate) =>
            new DelegatingFormSelection(doc => doc.Forms.Single(predicate));

        public static IFormSelection ByIndex(int index) =>
            new DelegatingFormSelection(doc => doc.Forms[index]);

        sealed class DelegatingFormSelection : IFormSelection
        {
            readonly Func<ParsedHtml, HtmlForm> _delegatee;

            public DelegatingFormSelection(Func<ParsedHtml, HtmlForm> delegatee) =>
                _delegatee = delegatee;

            public HtmlForm Select(ParsedHtml document) =>
                _delegatee(document);
        }
    }
}
