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

    public static class Query
    {
        public static Query<T> Return<T>(T value) =>
            new Query<T>(context => new QueryResult<T>(context, value));

        public static Query<string> DownloadString(Uri url) =>
            DownloadString(url, (_, s) => s);

        public static Query<T> DownloadString<T>(Uri url, Func<int, string, T> selector) =>
            new Query<T>(context => QueryResult.Create(new QueryContext(id: context.Id + 1,
                                                                        serviceProvider: context.ServiceProvider),
                                                       context.Eval((IWebClient wc) => selector(context.Id, wc.DownloadString(url)))));
    }
}
