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

using System;

namespace WebLinq;

public sealed record Cookie(string Name, string Value)
{
    public Cookie(System.Net.Cookie cookie) :
        this((cookie ?? throw new ArgumentNullException(nameof(cookie))).Name, cookie.Value)
    {
        Comment = cookie.Comment;
        CommentUri = cookie.CommentUri;
        Discard = cookie.Discard;
        Domain = cookie.Domain;
        Expired = cookie.Expired;
        Expires = cookie.Expires;
        HttpOnly = cookie.HttpOnly;
        Path = cookie.Path;
        Port = cookie.Port;
        Secure = cookie.Secure;
        TimeStamp = cookie.TimeStamp;
        Version = cookie.Version;
    }

    public string Comment { get; init; } = string.Empty;
    public Uri? CommentUri { get; init; }
    public bool Discard { get; init; }
    public string Domain { get; init; } = string.Empty;
    public bool Expired { get; init; }
    public DateTime Expires { get; init; }
    public bool HttpOnly { get; init; }
    public string Path { get; init; } = string.Empty;
    public string Port { get; init; } = string.Empty;
    public bool Secure { get; init; }
    public DateTime TimeStamp { get; init; }
    public int Version { get; init; }

    public System.Net.Cookie ToSystemNetCookie() =>
        new(Name, Value, Path, Domain)
        {
            Comment = Comment,
            CommentUri = CommentUri,
            Discard = Discard,
            Expired = Expired,
            Expires = Expires,
            HttpOnly = HttpOnly,
            Port = Port,
            Secure = Secure,
            Version = Version,
        };
}
