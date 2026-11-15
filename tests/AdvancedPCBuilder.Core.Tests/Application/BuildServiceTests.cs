using AdvancedPCBuilder.Core.Application;
using AdvancedPCBuilder.Core.Domain.Compatibility;
using AdvancedPCBuilder.Core.Tests.Fixtures;

namespace AdvancedPCBuilder.Core.Tests.Application;

public class BuildServiceTests
{
    private readonly FakeBuildRepository _repository = new();
    private readonly BuildService _service;

    public BuildServiceTests()
    {
        var checkService = new CompatibilityCheckService(new BuildResolver(new FakeComponentCatalog()), TestEngine.Create());
        _service = new BuildService(_repository, checkService);
    }

    [Fact]
    public async Task Create_ValidRequest_SavesBuildWithItsReport()
    {
        ValidationResult<SavedBuild> result = await _service.CreateAsync("Office PC", TestRequests.S1);

        Assert.True(result.IsValid);
        SavedBuild saved = Assert.Single(_repository.Builds.Values);
        Assert.Equal(result.Value!.Id, saved.Id);
        Assert.Equal("Office PC", saved.Name);
        Assert.Equal(CompatibilityStatus.Approved, saved.Report.Status);
        Assert.Equal(414, saved.Report.RequiredPsuWatts);
    }

    [Fact]
    public async Task Create_MissingNameAndCpu_ReportsBothErrors_AndSavesNothing()
    {
        ValidationResult<SavedBuild> result = await _service.CreateAsync(" ", TestRequests.S1 with { CpuId = null });

        Assert.False(result.IsValid);
        Assert.Equal(["cpuId", "name"], result.Errors.Keys.Order());
        Assert.Empty(_repository.Builds);
    }

    [Fact]
    public async Task Update_ReChecksTheBuild()
    {
        Guid id = (await _service.CreateAsync("Gaming PC", TestRequests.S1)).Value!.Id;

        ValidationResult<SavedBuild>? result = await _service.UpdateAsync(id, "Gaming PC v2", TestRequests.S7);

        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Equal("Gaming PC v2", _repository.Builds[id].Name);
        Assert.Equal(CompatibilityStatus.Rejected, _repository.Builds[id].Report.Status);
    }

    [Fact]
    public async Task Update_UnknownId_ReturnsNull_EvenWhenRequestIsInvalid()
    {
        ValidationResult<SavedBuild>? result = await _service.UpdateAsync(Guid.NewGuid(), null, null);

        Assert.Null(result);
        Assert.Empty(_repository.Builds);
    }

    [Fact]
    public async Task Update_InvalidRequest_KeepsTheOldBuild()
    {
        Guid id = (await _service.CreateAsync("Office PC", TestRequests.S1)).Value!.Id;

        ValidationResult<SavedBuild>? result = await _service.UpdateAsync(id, "Office PC", TestRequests.S1 with { PsuId = null });

        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Equal(CompatibilityStatus.Approved, _repository.Builds[id].Report.Status);
    }

    [Fact]
    public async Task Delete_RemovesBuild_UnknownIdReturnsFalse()
    {
        Guid id = (await _service.CreateAsync("Office PC", TestRequests.S1)).Value!.Id;

        Assert.True(await _service.DeleteAsync(id));
        Assert.False(await _service.DeleteAsync(id));
        Assert.Empty(_repository.Builds);
    }
}
