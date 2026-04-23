using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StrideFlow.Application.Models.Dashboard;
using StrideFlow.Application.Models.Sessions;
using StrideFlow.IntegrationTests.Fixtures;

namespace StrideFlow.IntegrationTests.Scenarios;

public sealed class TrackingAndLeaderboardTests(StrideFlowApiFactory factory) : IClassFixture<StrideFlowApiFactory>
{
    [Fact]
    public async Task Session_endpoints_live_map_and_city_leaderboard_work()
    {
        var anonymousClient = factory.CreateClient();
        var moscowWalker = await AuthTestHelper.RegisterAsync(anonymousClient, "Moscow");
        var kazanWalker = await AuthTestHelper.RegisterAsync(anonymousClient, "Kazan");

        var moscowClient = factory.CreateAuthorizedClient(moscowWalker.Tokens.AccessToken);
        var kazanClient = factory.CreateAuthorizedClient(kazanWalker.Tokens.AccessToken);

        var startResponse = await moscowClient.PostSnakeCaseAsync("/api/sessions", new StartSessionRequest("Morning walk"));
        startResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var started = await startResponse.Content.ReadSnakeCaseAsync<WalkingSessionDetailResponse>();
        started.Should().NotBeNull();

        var sessionId = started!.Session.Id;

        var currentResponse = await moscowClient.GetAsync("/api/sessions/current");
        currentResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var pointsResponse = await moscowClient.PostSnakeCaseAsync($"/api/sessions/{sessionId}/points", new TrackSessionPointsRequest(
        [
            new TrackPointRequest(55.7558, 37.6173, 10, null, DateTimeOffset.UtcNow.AddMinutes(-4)),
            new TrackPointRequest(55.7568, 37.6189, 8, null, DateTimeOffset.UtcNow.AddMinutes(-2)),
            new TrackPointRequest(55.7580, 37.6204, 7, null, DateTimeOffset.UtcNow)
        ]));
        pointsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tracked = await pointsResponse.Content.ReadSnakeCaseAsync<WalkingSessionDetailResponse>();
        tracked.Should().NotBeNull();
        tracked!.Session.TotalSteps.Should().BeGreaterThan(0);
        tracked.Points.Should().HaveCountGreaterThan(0);

        var getByIdResponse = await moscowClient.GetAsync($"/api/sessions/{sessionId}");
        getByIdResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var historyResponse = await moscowClient.GetAsync("/api/sessions");
        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var history = await historyResponse.Content.ReadSnakeCaseAsync<IReadOnlyList<WalkingSessionResponse>>();
        history.Should().NotBeNull();
        history!.Should().Contain(x => x.Id == sessionId);

        var pauseResponse = await moscowClient.PostAsync($"/api/sessions/{sessionId}/pause", null);
        pauseResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var resumeResponse = await moscowClient.PostAsync($"/api/sessions/{sessionId}/resume", null);
        resumeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var liveMapResponse = await moscowClient.GetAsync("/api/live/map");
        liveMapResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var liveMap = await liveMapResponse.Content.ReadSnakeCaseAsync<IReadOnlyList<LiveSessionResponse>>();
        liveMap.Should().NotBeNull();
        liveMap!.Should().Contain(x => x.SessionId == sessionId);

        var stopResponse = await moscowClient.PostAsync($"/api/sessions/{sessionId}/stop", null);
        stopResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var kazanStartResponse = await kazanClient.PostSnakeCaseAsync("/api/sessions", new StartSessionRequest("City challenge"));
        var kazanStarted = await kazanStartResponse.Content.ReadSnakeCaseAsync<WalkingSessionDetailResponse>();
        var kazanSessionId = kazanStarted!.Session.Id;

        await kazanClient.PostSnakeCaseAsync($"/api/sessions/{kazanSessionId}/points", new TrackSessionPointsRequest(
        [
            new TrackPointRequest(55.7961, 49.1064, 12, null, DateTimeOffset.UtcNow.AddMinutes(-3)),
            new TrackPointRequest(55.7974, 49.1083, 8, null, DateTimeOffset.UtcNow)
        ]));

        var moscowLeaderboardResponse = await moscowClient.GetAsync("/api/leaderboard?period=week&city=Moscow&limit=10");
        moscowLeaderboardResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var moscowLeaderboard = await moscowLeaderboardResponse.Content.ReadSnakeCaseAsync<LeaderboardResponse>();
        moscowLeaderboard.Should().NotBeNull();
        moscowLeaderboard!.City.Should().Be("Moscow");
        moscowLeaderboard.RewardPreview.Should().Contain(x => x.Rank == 1 && x.TelegramStars == 3);
        moscowLeaderboard.Entries.Should().ContainSingle(x => x.UserId == moscowWalker.User.Id);
        moscowLeaderboard.Entries.Should().NotContain(x => x.UserId == kazanWalker.User.Id);
    }
}
