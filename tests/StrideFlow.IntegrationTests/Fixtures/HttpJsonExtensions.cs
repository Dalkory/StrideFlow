using System.Net.Http.Json;

namespace StrideFlow.IntegrationTests.Fixtures;

public static class HttpJsonExtensions
{
    public static Task<HttpResponseMessage> PostSnakeCaseAsync<T>(this HttpClient client, string requestUri, T value)
    {
        return client.PostAsJsonAsync(requestUri, value, AuthTestHelper.SnakeCaseJson);
    }

    public static Task<HttpResponseMessage> PutSnakeCaseAsync<T>(this HttpClient client, string requestUri, T value)
    {
        return client.PutAsJsonAsync(requestUri, value, AuthTestHelper.SnakeCaseJson);
    }

    public static Task<T?> ReadSnakeCaseAsync<T>(this HttpContent content)
    {
        return content.ReadFromJsonAsync<T>(AuthTestHelper.SnakeCaseJson);
    }
}
