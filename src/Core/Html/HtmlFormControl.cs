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
    public sealed class HtmlFormControl
    {
        public HtmlObject Element { get; }
        public string Name { get; }
        public HtmlControlType ControlType { get; }
        public HtmlInputType InputType { get; }
        public HtmlDisabledFlag Disabled { get; }
        public HtmlReadOnlyFlag ReadOnly { get; }

        internal HtmlFormControl(HtmlObject element, string name, HtmlControlType controlType, HtmlInputType inputType, HtmlDisabledFlag disabled, HtmlReadOnlyFlag readOnly)
        {
            Element     = element;
            Name        = name;
            ControlType = controlType;
            InputType   = inputType;
            Disabled    = disabled;
            ReadOnly    = readOnly;
        }

        public override string ToString() => Element.ToString();
    }
}