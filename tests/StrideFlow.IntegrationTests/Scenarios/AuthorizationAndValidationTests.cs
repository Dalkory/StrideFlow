using System.Net;
using FluentAssertions;
using StrideFlow.Application.Models.Auth;
using StrideFlow.Application.Models.Sessions;
using StrideFlow.IntegrationTests.Fixtures;

namespace StrideFlow.IntegrationTests.Scenarios;

public sealed class AuthorizationAndValidationTests(StrideFlowApiFactory factory) : IClassFixture<StrideFlowApiFactory>
{
    [Theory]
    [InlineData("/api/auth/me")]
    [InlineData("/api/profile")]
    [InlineData("/api/dashboard")]
    [InlineData("/api/sessions")]
    [InlineData("/api/sessions/current")]
    [InlineData("/api/leaderboard")]
    [InlineData("/api/live/map")]
    [InlineData("/api/ads/slots")]
    [InlineData("/api/rewards/summary")]
    [InlineData("/api/insights")]
    public async Task Protected_get_endpoints_require_authorization(string endpoint)
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync(endpoint);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_validation_returns_problem_details()
    {
        var client = factory.CreateClient();

        var response = await client.PostSnakeCaseAsync("/api/auth/register", new RegisterRequest(
            "not-an-email",
            "x",
            "",
            "weak",
            80,
            20,
            100,
            "",
            ""));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var contentType = response.Content.Headers.ContentType?.MediaType;
        contentType.Should().Contain("problem");
    }

    [Fact]
    public async Task Domain_errors_return_expected_status_codes()
    {
        var anonymousClient = factory.CreateClient();
        var registration = await AuthTestHelper.RegisterAsync(anonymousClient, "Moscow");
        var client = factory.CreateAuthorizedClient(registration.Tokens.AccessToken);

        var invalidPeriodResponse = await client.GetAsync("/api/leaderboard?period=year");
        invalidPeriodResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var startResponse = await client.PostSnakeCaseAsync("/api/sessions", new StartSessionRequest("First walk"));
        startResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var duplicateStartResponse = await client.PostSnakeCaseAsync("/api/sessions", new StartSessionRequest("Second walk"));
        duplicateStartResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var session = await startResponse.Content.ReadSnakeCaseAsync<WalkingSessionDetailResponse>();
        var stopResponse = await client.PostAsync($"/api/sessions/{session!.Session.Id}/stop", null);
        stopResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var addAfterStopResponse = await client.PostSnakeCaseAsync($"/api/sessions/{session.Session.Id}/points", new TrackSessionPointsRequest(
        [
            new TrackPointRequest(55.7558, 37.6173, 8, null, DateTimeOffset.UtcNow)
        ]));
        addAfterStopResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task SignalR_activity_hub_negotiate_requires_jwt_and_accepts_authorized_clients()
    {
        var anonymousClient = factory.CreateClient();

        var unauthorizedNegotiate = await anonymousClient.PostAsync("/hubs/activity/negotiate?negotiateVersion=1", null);
        unauthorizedNegotiate.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var registration = await AuthTestHelper.RegisterAsync(anonymousClient, "Moscow");
        var authorizedClient = factory.CreateAuthorizedClient(registration.Tokens.AccessToken);

        var authorizedNegotiate = await authorizedClient.PostAsync("/hubs/activity/negotiate?negotiateVersion=1", null);
        authorizedNegotiate.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await authorizedNegotiate.Content.ReadAsStringAsync();
        body.Should().Contain("availableTransports");
    }
}
