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

public sealed class HttpQuerySetup
{
    public static readonly HttpQuerySetup Default = new(HttpOptions.Default, config => config, _ => true);

    public HttpQuerySetup(HttpOptions options,
                          Func<HttpConfig, HttpConfig> configurer,
                          Func<HttpFetchInfo, bool> filterPredicate)
    {
        Options = options;
        Configurer = configurer;
        FilterPredicate = filterPredicate;
    }

    HttpQuerySetup(HttpQuerySetup other) :
        this(other.Options, other.Configurer, other.FilterPredicate) { }

    public HttpOptions Options { get; private init; }
    public Func<HttpConfig, HttpConfig> Configurer { get; private init; }
    public Func<HttpFetchInfo, bool> FilterPredicate { get; private init; }

    public HttpQuerySetup WithOptions(HttpOptions value) =>
        value == Options ? this : new(this) { Options = value };

    public HttpQuerySetup WithConfigurer(Func<HttpConfig, HttpConfig> value) =>
        value == Configurer ? this : new(this) { Configurer = value };

    public HttpQuerySetup WithFilterPredicate(Func<HttpFetchInfo, bool> value) =>
        value == FilterPredicate ? this : new(this) { FilterPredicate = value };
}
