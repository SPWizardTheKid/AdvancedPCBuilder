using System.Net;
using System.Net.Http.Json;
using AdvancedPCBuilder.Api.Contracts;
using AdvancedPCBuilder.Api.Tests.Fixtures;
using AdvancedPCBuilder.Core.Domain.Compatibility;
using Microsoft.AspNetCore.Http;

namespace AdvancedPCBuilder.Api.Tests;

/// <summary>POST /api/compatibility/check end to end: HTTP → middleware → service → engine → SQLite catalog.</summary>
public class CompatibilityApiTests : IClassFixture<ApiFactory>
{
    private const string CheckUrl = "/api/compatibility/check";

    private readonly HttpClient _techshop;

    public CompatibilityApiTests(ApiFactory factory)
    {
        _techshop = factory.CreateTenantClient(ApiFactory.TechShop);
    }

    [Fact]
    public async Task S1_NormalBuild_Returns200_Approved_Required414W()
    {
        HttpResponseMessage response = await _techshop.PostAsJsonAsync(CheckUrl, TestRequests.S1);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<CompatibilityReportResponse>(ApiFactory.Json);
        Assert.NotNull(report);
        Assert.Equal(CompatibilityStatus.Approved, report.Status);
        Assert.Equal(414, report.RequiredPsuWatts);
        Assert.Empty(report.Issues);
    }

    [Fact]
    public async Task S7_MultipleErrors_Returns200_Rejected_WithR2AndR5()
    {
        HttpResponseMessage response = await _techshop.PostAsJsonAsync(CheckUrl, TestRequests.S7);

        // A rejected build is a business result, not an HTTP error.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<CompatibilityReportResponse>(ApiFactory.Json);
        Assert.NotNull(report);
        Assert.Equal(CompatibilityStatus.Rejected, report.Status);
        Assert.Equal(600, report.RequiredPsuWatts);
        Assert.Equal([RuleCodes.CoolerHeight, RuleCodes.PsuPower], report.Issues.Select(i => i.Rule));

        IssueResponse cooler = report.Issues[0];
        Assert.Equal(Severity.Error, cooler.Severity);
        Assert.Equal(5, cooler.Difference);

        IssueResponse psu = report.Issues[1];
        Assert.Equal(Severity.Error, psu.Severity);
        Assert.Equal(150, psu.Difference);
    }

    [Fact]
    public async Task MissingCpuId_Returns400_WithCpuIdError()
    {
        HttpResponseMessage response = await _techshop.PostAsJsonAsync(CheckUrl, TestRequests.S1 with { CpuId = null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(ApiFactory.Json);
        Assert.NotNull(problem);
        Assert.Equal(["cpuId"], problem.Errors.Keys);
    }
}
