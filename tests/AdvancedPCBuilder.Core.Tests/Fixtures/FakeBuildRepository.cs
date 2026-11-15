using AdvancedPCBuilder.Core.Application;

namespace AdvancedPCBuilder.Core.Tests.Fixtures;

/// <summary>In-memory saved builds of a single tenant, for application tests.</summary>
public sealed class FakeBuildRepository : IBuildRepository
{
    public Dictionary<Guid, SavedBuild> Builds { get; } = new();

    public Task<IReadOnlyList<SavedBuild>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SavedBuild>>(Builds.Values.ToList());

    public Task<SavedBuild?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Builds.GetValueOrDefault(id));

    public Task AddAsync(SavedBuild build, CancellationToken cancellationToken = default)
    {
        Builds.Add(build.Id, build);
        return Task.CompletedTask;
    }

    public Task<bool> UpdateAsync(SavedBuild build, CancellationToken cancellationToken = default)
    {
        if (!Builds.ContainsKey(build.Id))
        {
            return Task.FromResult(false);
        }

        Builds[build.Id] = build;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Builds.Remove(id));
}
