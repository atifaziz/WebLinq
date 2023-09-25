#region Copyright (c) 2023 Atif Aziz. All rights reserved.
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

using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace WebLinq.Json;

public static class JsonContentReader
{
    public static IHttpContentReader<T?> Deserialize<T>() => Deserialize<T>(null);

    public static IHttpContentReader<T?> Deserialize<T>(JsonSerializerOptions? options) =>
        from json in HttpContentReader.Text()
        select JsonSerializer.Deserialize<T?>(json, options);

    public static IHttpContentReader<T?> Utf8Array<T>() => Utf8Array<T>(null);

    public static IHttpContentReader<T?> Utf8Array<T>(JsonSerializerOptions? options) =>
        HttpContentReader.Create((_, content, cancellationToken) =>
        {
            return Iterator();

            async IAsyncEnumerator<T?> Iterator()
            {
                var stream = await content.ReadAsStreamAsync(cancellationToken)
                                          .ConfigureAwait(false);
                await using (stream.ConfigureAwait(false))
                {
                    var items = JsonSerializer.DeserializeAsyncEnumerable<T>(stream, options, cancellationToken: cancellationToken);
                    await foreach (var item in items)
                        yield return item;
                }
            }
        });
}
