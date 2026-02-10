namespace INWC.Automation.Cli.Domain.Models;

internal sealed record AppDefinition(
    string Name,
    string[] Candidates,
    string[] Patterns,
    string LocalProductToken);

internal sealed record ConfigResolutionRow(
    string ProductRoot,
    string ConfigSetup,
    bool Exists,
    string? RawCustom,
    string? NormalizedCustom,
    string? EffectiveCustom,
    string? IncludePath,
    bool IncludeResolves);

internal sealed record SoftwareSignal(
    string Source,
    string FilePath,
    DateTime TimestampUtc);
