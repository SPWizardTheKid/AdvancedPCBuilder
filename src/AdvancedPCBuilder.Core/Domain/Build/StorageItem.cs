using AdvancedPCBuilder.Core.Domain.Components;

namespace AdvancedPCBuilder.Core.Domain.Build;

/// <summary>One storage line of a build: a drive model and how many of it.</summary>
public sealed record StorageItem(StorageSpec Spec, int Quantity);
