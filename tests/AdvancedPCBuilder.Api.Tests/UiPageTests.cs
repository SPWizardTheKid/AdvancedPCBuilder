using System.Net;

namespace AdvancedPCBuilder.Api.Tests;

/// <summary>The static demo page (wwwroot/index.html) is served at "/" and needs no tenant header.</summary>
public class UiPageTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public UiPageTests(ApiFactory factory)
    {
        _client = factory.CreateClient();   // no X-Tenant-Id: only /api/* requires it
    }

    [Fact]
    public async Task Root_ServesTheDemoPage_WithoutTenantHeader()
    {
        HttpResponseMessage response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        string html = await response.Content.ReadAsStringAsync();
        Assert.Contains("<title>AdvancedPCBuilder</title>", html);
    }
}
