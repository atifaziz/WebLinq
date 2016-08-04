#region Copyright(c) .NET Foundation and Contributors. All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License"); you
// may not use this file except in compliance with the License. You may
// obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
// implied. See the License for the specific language governing permissions
// and limitations under the License.
//
#endregion

namespace WebLinq
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// A type that indicates the absence of a specific value. It has only a
    /// single value, which acts as a placeholder when no other value exists or
    /// is needed.
    /// </summary>

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Size = 1)]
    public struct Unit : IEquatable<Unit>
    {
        public static bool operator ==(Unit a, Unit b) => true;
        public static bool operator !=(Unit a, Unit b) => false;
        public bool Equals(Unit other) => true;
        public override bool Equals(object obj) => obj is Unit;
        public override int GetHashCode() => 0;
    }
}
