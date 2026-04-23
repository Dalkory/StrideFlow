using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StrideFlow.Application.Models.Auth;
using StrideFlow.IntegrationTests.Fixtures;

namespace StrideFlow.IntegrationTests.Scenarios;

public sealed class AuthEndpointsTests(StrideFlowApiFactory factory) : IClassFixture<StrideFlowApiFactory>
{
    [Fact]
    public async Task Register_returns_tokens_and_profile()
    {
        var client = factory.CreateClient();

        var response = await AuthTestHelper.RegisterAsync(client, "Moscow");

        response.Tokens.AccessToken.Should().NotBeNullOrWhiteSpace();
        response.Tokens.RefreshToken.Should().NotBeNullOrWhiteSpace();
        response.User.City.Should().Be("Moscow");
        response.User.Email.Should().Contain("@strideflow.dev");
    }

    [Fact]
    public async Task Login_refresh_me_and_logout_flow_works()
    {
        var client = factory.CreateClient();
        var registration = await AuthTestHelper.RegisterAsync(client, "Kazan");

        var loginResponseMessage = await client.PostSnakeCaseAsync("/api/auth/login", new LoginRequest(
            registration.User.Email,
            "StrongPass1",
            "Integration iPhone"));
        loginResponseMessage.StatusCode.Should().Be(HttpStatusCode.OK);

        var login = await loginResponseMessage.Content.ReadSnakeCaseAsync<AuthResponse>();
        login.Should().NotBeNull();

        var authorizedClient = factory.CreateAuthorizedClient(login!.Tokens.AccessToken);
        var me = await AuthTestHelper.GetMeAsync(authorizedClient);
        me.Email.Should().Be(registration.User.Email);
        me.City.Should().Be("Kazan");

        var refreshResponseMessage = await client.PostSnakeCaseAsync("/api/auth/refresh", new RefreshTokenRequest(login.Tokens.RefreshToken));
        refreshResponseMessage.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshed = await refreshResponseMessage.Content.ReadSnakeCaseAsync<AuthResponse>();
        refreshed.Should().NotBeNull();
        refreshed!.Tokens.RefreshToken.Should().NotBe(login.Tokens.RefreshToken);

        var logoutResponse = await authorizedClient.PostSnakeCaseAsync("/api/auth/logout", new LogoutRequest(refreshed.Tokens.RefreshToken));
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
