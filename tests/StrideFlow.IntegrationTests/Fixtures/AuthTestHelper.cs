using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using StrideFlow.Application.Models.Auth;
using StrideFlow.Application.Models.Users;

namespace StrideFlow.IntegrationTests.Fixtures;

public static class AuthTestHelper
{
    public static readonly JsonSerializerOptions SnakeCaseJson = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public static async Task<AuthResponse> RegisterAsync(HttpClient client, string city = "Moscow")
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var request = new RegisterRequest(
            $"walker-{suffix}@strideflow.dev",
            $"walker_{suffix}",
            $"Walker {suffix}",
            "StrongPass1",
            182,
            78,
            12000,
            city,
            "Europe/Moscow");

        var response = await client.PostAsJsonAsync("/api/auth/register", request, SnakeCaseJson);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Registration failed with status {(int)response.StatusCode}: {errorBody}");
        }

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>(SnakeCaseJson);
        body.Should().NotBeNull();

        return body!;
    }

    public static HttpClient CreateAuthorizedClient(this StrideFlowApiFactory factory, string accessToken)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    public static async Task<UserProfileResponse> GetMeAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/auth/me");
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Get /api/auth/me failed with status {(int)response.StatusCode}: {errorBody}");
        }

        var body = await response.Content.ReadFromJsonAsync<UserProfileResponse>(SnakeCaseJson);
        body.Should().NotBeNull();
        return body!;
    }
}
