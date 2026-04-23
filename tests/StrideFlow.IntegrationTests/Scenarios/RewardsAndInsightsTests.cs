using System.Net;
using FluentAssertions;
using StrideFlow.Application.Models.Engagement;
using StrideFlow.Application.Models.Sessions;
using StrideFlow.IntegrationTests.Fixtures;

namespace StrideFlow.IntegrationTests.Scenarios;

public sealed class RewardsAndInsightsTests(StrideFlowApiFactory factory) : IClassFixture<StrideFlowApiFactory>
{
    [Fact]
    public async Task Rewards_summary_and_activity_insights_reflect_city_rank_and_achievements()
    {
        var anonymousClient = factory.CreateClient();
        var topWalker = await AuthTestHelper.RegisterAsync(anonymousClient, "Moscow");
        var secondWalker = await AuthTestHelper.RegisterAsync(anonymousClient, "Moscow");
        var outsideWalker = await AuthTestHelper.RegisterAsync(anonymousClient, "Kazan");

        var topClient = factory.CreateAuthorizedClient(topWalker.Tokens.AccessToken);
        var secondClient = factory.CreateAuthorizedClient(secondWalker.Tokens.AccessToken);
        var outsideClient = factory.CreateAuthorizedClient(outsideWalker.Tokens.AccessToken);

        await CompleteWalkAsync(topClient, "Long city route", 55.7558, 37.6173, 5);
        await CompleteWalkAsync(secondClient, "Short city route", 55.7522, 37.6156, 2);
        await CompleteWalkAsync(outsideClient, "Outside city route", 55.7961, 49.1064, 6);

        var rewardsResponse = await topClient.GetAsync("/api/rewards/summary");
        rewardsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var rewards = await rewardsResponse.Content.ReadSnakeCaseAsync<RewardSummaryResponse>();
        rewards.Should().NotBeNull();
        rewards!.PayoutProvider.Should().Be("telegram_stars");
        rewards.IsTestMode.Should().BeTrue();
        rewards.Weekly.City.Should().Be("Moscow");
        rewards.Weekly.Rank.Should().Be(1);
        rewards.Weekly.IsEligible.Should().BeTrue();
        rewards.Weekly.TelegramStars.Should().Be(3);
        rewards.Monthly.Rank.Should().Be(1);
        rewards.Monthly.TelegramStars.Should().Be(10);

        var secondRewardsResponse = await secondClient.GetAsync("/api/rewards/summary");
        var secondRewards = await secondRewardsResponse.Content.ReadSnakeCaseAsync<RewardSummaryResponse>();
        secondRewards!.Weekly.Rank.Should().Be(2);
        secondRewards.Weekly.TelegramStars.Should().Be(2);

        var insightsResponse = await topClient.GetAsync("/api/insights");
        insightsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var insights = await insightsResponse.Content.ReadSnakeCaseAsync<ActivityInsightsResponse>();
        insights.Should().NotBeNull();
        insights!.Coach.Achievements.Should().NotBeEmpty();
        insights.Coach.WeeklyAverageSteps.Should().BeGreaterThan(0);
        insights.Coach.Message.Should().Contain("Reward preview");
        insights.Rewards.Weekly.Rank.Should().Be(1);
        insights.Rewards.Weekly.TelegramStars.Should().Be(3);
    }

    private static async Task CompleteWalkAsync(HttpClient client, string name, double latitude, double longitude, int pointCount)
    {
        var startResponse = await client.PostSnakeCaseAsync("/api/sessions", new StartSessionRequest(name));
        startResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var started = await startResponse.Content.ReadSnakeCaseAsync<WalkingSessionDetailResponse>();
        var sessionId = started!.Session.Id;
        var startAt = DateTimeOffset.UtcNow.AddMinutes(-pointCount * 3);
        var points = Enumerable.Range(0, pointCount)
            .Select(index => new TrackPointRequest(
                latitude + index * 0.0008,
                longitude + index * 0.0011,
                8,
                null,
                startAt.AddMinutes(index * 3)))
            .ToArray();

        var pointsResponse = await client.PostSnakeCaseAsync($"/api/sessions/{sessionId}/points", new TrackSessionPointsRequest(points));
        pointsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var stopResponse = await client.PostAsync($"/api/sessions/{sessionId}/stop", null);
        stopResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
