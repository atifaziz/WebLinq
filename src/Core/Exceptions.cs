#region Copyright (c) 2022 Atif Aziz. All rights reserved.
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

namespace WebLinq;

using System;

public sealed class UnacceptableMediaException : Exception
{
    const string DefaultMessage = "Content media type is unacceptable.";

    public UnacceptableMediaException() : this(null) { }
    public UnacceptableMediaException(string? message) : this(message, null) { }
    public UnacceptableMediaException(string? message, Exception? inner) : base(message ?? DefaultMessage, inner) { }
}

public class ElementNotFoundException : Exception
{
    const string DefaultMessage = "Element not found.";

    public ElementNotFoundException() : this(null) { }
    public ElementNotFoundException(string? message) : this(message, null) { }
    public ElementNotFoundException(string? message, Exception? inner) : base(message ?? DefaultMessage, inner) { }
}
