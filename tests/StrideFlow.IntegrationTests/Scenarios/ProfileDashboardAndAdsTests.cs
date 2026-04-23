using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StrideFlow.Application.Models.Ads;
using StrideFlow.Application.Models.Dashboard;
using StrideFlow.Application.Models.Users;
using StrideFlow.IntegrationTests.Fixtures;

namespace StrideFlow.IntegrationTests.Scenarios;

public sealed class ProfileDashboardAndAdsTests(StrideFlowApiFactory factory) : IClassFixture<StrideFlowApiFactory>
{
    [Fact]
    public async Task Profile_dashboard_ads_and_health_endpoints_work()
    {
        var anonymousClient = factory.CreateClient();
        var registration = await AuthTestHelper.RegisterAsync(anonymousClient, "Moscow");
        var client = factory.CreateAuthorizedClient(registration.Tokens.AccessToken);

        var profileResponse = await client.GetAsync("/api/profile");
        profileResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await profileResponse.Content.ReadSnakeCaseAsync<UserProfileResponse>();
        profile.Should().NotBeNull();
        profile!.City.Should().Be("Moscow");

        var updateResponse = await client.PutSnakeCaseAsync("/api/profile", new UpdateProfileRequest(
            "Stride Walker",
            "Testing the leaderboard flow.",
            "Saint Petersburg",
            "Europe/Moscow",
            "#457b9d",
            180,
            74,
            0.76,
            14000,
            true));
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedProfile = await updateResponse.Content.ReadSnakeCaseAsync<UserProfileResponse>();
        updatedProfile!.City.Should().Be("Saint Petersburg");
        updatedProfile.DailyStepGoal.Should().Be(14000);

        var dashboardResponse = await client.GetAsync("/api/dashboard");
        dashboardResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var dashboard = await dashboardResponse.Content.ReadSnakeCaseAsync<DashboardResponse>();
        dashboard.Should().NotBeNull();
        dashboard!.User.City.Should().Be("Saint Petersburg");
        dashboard.AdSlots.Should().NotBeEmpty();
        dashboard.Leaderboard.City.Should().Be("Saint Petersburg");

        var adsResponse = await client.GetAsync("/api/ads/slots");
        adsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var ads = await adsResponse.Content.ReadSnakeCaseAsync<IReadOnlyList<AdSlotResponse>>();
        ads.Should().NotBeNull();
        ads!.Should().Contain(x => x.Key == "dashboard_hero" && x.Enabled);

        var healthResponse = await anonymousClient.GetAsync("/health");
        healthResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
