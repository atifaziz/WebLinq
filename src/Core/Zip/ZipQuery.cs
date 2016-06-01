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

namespace WebLinq.Zip
{
    using System.IO;
    using System.Net.Http;
    using System.Net.Mime;

    public static class ZipQuery
    {
        public static Query<HttpFetch<Zip>> Unzip(this Query<HttpFetch<HttpContent>> query) =>
            query.Accept(MediaTypeNames.Application.Zip)
                 .Bind(fetch =>
                 {
                     return Query.Create(context =>
                     {
                         var path = Path.GetTempFileName();
                         using (var output = File.Create(path))
                             fetch.Content.CopyToAsync(output).Wait();
                         return QueryResult.Create(context, fetch.WithContent(new Zip(path)));
                     });
                 });
    }
}
