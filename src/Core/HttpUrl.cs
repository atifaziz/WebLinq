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

namespace WebLinq
{
    using System;
    using Collections;

    public enum HttpProtocol { Http, Https }

    /// <summary>
    /// Represents an absolute URI that follows the HTTP or HTTPS scheme.
    /// </summary>

    public struct HttpUrl : IEquatable<HttpUrl>, IEquatable<Uri>
    {
        readonly Uri _uri;

        public HttpUrl(string uri) :
            this(Parse(uri)) {}

        public HttpUrl(Uri uri)
        {
            _uri = uri is Uri some && !IsValid(some)
                 ? throw new ArgumentException(null, nameof(uri))
                 : uri ?? throw new ArgumentNullException(nameof(uri));
            _pathSegments = default;
        }

        public static HttpUrl From(HttpProtocol protocol, string host,
                                   string path = null, string query = null,
                                   string fragment = null) =>
            From(protocol, host, 0, path, query, fragment);

        public static HttpUrl From(HttpProtocol protocol, string host, int port,
                                   string path = null, string query = null,
                                   string fragment = null)
        {
            var builder = new UriBuilder
            {
                Scheme   = protocol == HttpProtocol.Https
                         ? Uri.UriSchemeHttps
                         : Uri.UriSchemeHttp,
                Host     = host,
                Port     = port,
                Path     = path,
                Query    = query,
                Fragment = fragment
            };

            return new HttpUrl(builder.Uri);
        }

        public static HttpUrl Format(FormattableString formattableString)
            => formattableString == null
             ? throw new ArgumentNullException(nameof(formattableString))
             : new HttpUrl(UriFormatter.Format(formattableString));

        public static bool IsValid(Uri uri)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            return uri.IsAbsoluteUri
                && (uri.Scheme != Uri.UriSchemeHttp ||
                    uri.Scheme != Uri.UriSchemeHttps);
        }

        public static HttpUrl Parse(string input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            return new HttpUrl(new Uri(input, UriKind.Absolute));
        }

        public static bool TryParse(string input, out HttpUrl url)
        {
            if (Uri.TryCreate(input, UriKind.Absolute, out var uri)
                && IsValid(uri))
            {
                url = new HttpUrl(uri);
                return true;
            }

            url = default;
            return false;
        }

        public HttpProtocol Protocol
            => _uri?.Scheme is string scheme
               && scheme == Uri.UriSchemeHttps
             ? HttpProtocol.Https
             : HttpProtocol.Http;

        public string UriScheme
            => _uri == null ? string.Empty
             : Protocol == HttpProtocol.Https ? Uri.UriSchemeHttps
             : Uri.UriSchemeHttp;

        public string UserInfo => _uri?.UserInfo ?? string.Empty;
        public string User     => _uri?.UserInfo.Split2(':').Item1 ?? string.Empty;
        public string Password => _uri?.UserInfo.Split2(':').Item2 ?? string.Empty;

        public string Host => _uri?.Host ?? string.Empty;
        public int    Port => _uri?.Port ?? 0;

        public string      Path       => _uri?.AbsolutePath ?? string.Empty;
        public QueryString Query      => _uri?.Query is string s ? new QueryString(s) : default;
        public string      Fragment   => _uri?.Fragment ?? string.Empty;

        public HttpUrl WithUserInfo(string userInfo)
        {
            if (_uri == null)
                throw new InvalidOperationException();
            if (string.IsNullOrEmpty(userInfo))
                return string.IsNullOrEmpty(UserInfo) ? this : WithUserInfo(null, null);
            var (user, password) = userInfo.Split2(':');
            return WithUserInfo(user, password);
        }

        public HttpUrl WithUserInfo(string user, string password)
        {
            if (_uri == null)
                throw new InvalidOperationException();
            if ((user ?? string.Empty) == User && (password ?? string.Empty) == Password)
                return this;
            //
            // The BCL types UriBuilder and Uri have some API inconsistencies.
            // While UriBuilder separates user-information components such as
            // UserName and Password, Uri doesn't. There are also
            // implementation inconsistencies like Uri being happy with
            // "http://:secret@example.com/" but UriBuilder thinks it's invalid.
            // So use UriBuilder to blow away the user-information component
            // completely and then inject it into the Uri manually.
            //
            var builder = new UriBuilder(this)
            {
                UserName = string.Empty,
                Password = string.Empty
            };
            var uri = builder.Uri.AbsoluteUri;
            const string colonWhackWhack = "://";
            var (head, tail) = uri.Split2(colonWhackWhack);
            var url = head
                    + colonWhackWhack
                    + user
                    + (string.IsNullOrEmpty(password) ? string.Empty : ":" + password)
                    + "@"
                    + tail;
            return new HttpUrl(url);
        }

        (bool, Strings) _pathSegments;

        public Strings PathSegments
            => _uri == null
             ? Strings.Empty
             : this.LazyGet(ref _pathSegments, it => Strings.Array(it._uri.Segments));

        public override string ToString() =>
            _uri?.AbsoluteUri ?? string.Empty;

        public bool Equals(HttpUrl other) =>
            Equals(other._uri);

        public bool Equals(Uri other) =>
            _uri == other;

        public override bool Equals(object obj)
        {
            switch (obj)
            {
                case null         : return _uri == null;
                case HttpUrl other: return Equals(other);
                case Uri other    : return Equals(other);
                default           : return false;
            }
        }

        public override int GetHashCode() =>
            _uri?.GetHashCode() ?? 0;

        public static bool operator ==(HttpUrl left, HttpUrl right) => left.Equals(right);
        public static bool operator !=(HttpUrl left, HttpUrl right) => !left.Equals(right);

        public static bool operator ==(HttpUrl left, Uri right) => left.Equals(right);
        public static bool operator !=(HttpUrl left, Uri right) => !left.Equals(right);

        public static bool operator ==(Uri left, HttpUrl right) => right.Equals(left);
        public static bool operator !=(Uri left, HttpUrl right) => !right.Equals(left);

        public static implicit operator Uri(HttpUrl url) => url._uri;
    }
}
