using System.Net;
using System.Net.Http.Json;
using AdvancedPCBuilder.Api.Contracts;
using AdvancedPCBuilder.Api.Tests.Fixtures;
using AdvancedPCBuilder.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace AdvancedPCBuilder.Api.Tests;

/// <summary>
/// Tenant isolation through the HTTP API. pcmeistrai tries to read and change techshop's data
/// (seeded build 1111…, ts- components) and must fail exactly as if the data did not exist.
/// Nothing here depends on a UI: every call goes straight to the API.
/// </summary>
public class TenantIsolationTests : IClassFixture<ApiFactory>
{
    private static readonly string TechShopBuildUrl = $"/api/builds/{SeedData.TechShopBuildId}";

    private readonly ApiFactory _factory;
    private readonly HttpClient _techshop;
    private readonly HttpClient _pcmeistrai;

    public TenantIsolationTests(ApiFactory factory)
    {
        _factory = factory;
        _techshop = factory.CreateTenantClient(ApiFactory.TechShop);
        _pcmeistrai = factory.CreateTenantClient(ApiFactory.PcMeistrai);
    }

    // ---- Read isolation ----

    [Fact]
    public async Task PcMeistrai_ComponentList_ContainsNoTechShopComponents()
    {
        List<ComponentResponse> components = await GetComponentsAsync(_pcmeistrai);

        Assert.Equal(10, components.Count);
        Assert.All(components, c => Assert.StartsWith("pm-", c.Id));
    }

    [Fact]
    public async Task TechShop_ComponentList_ContainsNoPcMeistraiComponents()
    {
        List<ComponentResponse> components = await GetComponentsAsync(_techshop);

        Assert.Equal(29, components.Count);
        Assert.All(components, c => Assert.StartsWith("ts-", c.Id));
    }

    [Fact]
    public async Task PcMeistrai_GetTechShopComponent_Returns404()
    {
        HttpResponseMessage response = await _pcmeistrai.GetAsync("/api/components/ts-cpu-65-am5");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PcMeistrai_BuildList_ContainsOnlyItsOwnBuild()
    {
        var builds = await _pcmeistrai.GetFromJsonAsync<List<SavedBuildResponse>>("/api/builds", ApiFactory.Json);

        SavedBuildResponse build = Assert.Single(builds!);
        Assert.Equal(SeedData.PcMeistraiBuildId, build.Id);
    }

    [Fact]
    public async Task PcMeistrai_GetTechShopBuild_Returns404()
    {
        HttpResponseMessage response = await _pcmeistrai.GetAsync(TechShopBuildUrl);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- Write isolation ----

    [Fact]
    public async Task PcMeistrai_UpdateTechShopBuild_Returns404_AndBuildIsUnchanged()
    {
        string before = await _techshop.GetStringAsync(TechShopBuildUrl);

        // The body is VALID for pcmeistrai, so the 404 cannot come from validation.
        HttpResponseMessage response = await _pcmeistrai.PutAsJsonAsync(
            TechShopBuildUrl, new SaveBuildRequest("Hacked", TestRequests.PcMeistraiS1));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        string after = await _techshop.GetStringAsync(TechShopBuildUrl);
        Assert.Equal(before, after);   // same name, request, report and UpdatedAt
    }

    [Fact]
    public async Task PcMeistrai_DeleteTechShopBuild_Returns404_AndBuildStillExists()
    {
        HttpResponseMessage response = await _pcmeistrai.DeleteAsync(TechShopBuildUrl);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        HttpResponseMessage techshopGet = await _techshop.GetAsync(TechShopBuildUrl);
        Assert.Equal(HttpStatusCode.OK, techshopGet.StatusCode);
    }

    // ---- Another tenant's components cannot be used in a check ----

    [Fact]
    public async Task PcMeistrai_CheckWithTechShopComponentIds_Returns400_NotFound()
    {
        HttpResponseMessage response = await _pcmeistrai.PostAsJsonAsync("/api/compatibility/check", TestRequests.S1);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(ApiFactory.Json);
        Assert.NotNull(problem);
        // Every ts- ID is reported, with the same "not found" text an unknown ID gets.
        Assert.Equal(
            ["caseId", "cpuCoolerId", "cpuId", "gpuId", "motherboardId", "psuId", "ram.id", "storage[0].id"],
            problem.Errors.Keys.Order());
        Assert.Contains("not found", Assert.Single(problem.Errors["cpuId"]));
    }

    // ---- Tenant must be known ----

    [Fact]
    public async Task NoTenantHeader_Returns401()
    {
        HttpClient anonymous = _factory.CreateClient();

        HttpResponseMessage response = await anonymous.GetAsync("/api/components");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UnknownTenant_Returns401()
    {
        HttpClient unknown = _factory.CreateTenantClient("no-such-shop");

        HttpResponseMessage response = await unknown.GetAsync("/api/components");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<List<ComponentResponse>> GetComponentsAsync(HttpClient client)
    {
        var components = await client.GetFromJsonAsync<List<ComponentResponse>>("/api/components", ApiFactory.Json);
        Assert.NotNull(components);
        return components;
    }
}
