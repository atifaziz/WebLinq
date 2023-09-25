namespace WebLinq.Json;

using System;
using System.Text.Json;

public static class HttpQueryExtensions
{
    public static IHttpQuery<HttpFetch<T?>> Json<T>(this IHttpQuery query) =>
        Json<T>(query, null);

    public static IHttpQuery<HttpFetch<T?>> Json<T>(this IHttpQuery query, JsonSerializerOptions? options)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));

        return query.ReadContent(JsonContentReader.Deserialize<T>(options));
    }

    public static IHttpQuery<HttpFetch<T?>> Utf8JsonArray<T>(this IHttpQuery query) =>
        Utf8JsonArray<T>(query, null);

    public static IHttpQuery<HttpFetch<T?>> Utf8JsonArray<T>(this IHttpQuery query, JsonSerializerOptions? options)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));

        return query.ReadContent(JsonContentReader.Utf8Array<T>(options));
    }
}
