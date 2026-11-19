using System.Text.Json;
using System.Text.Json.Serialization;
using WayPlot.domain.Common;

namespace WayPlot.domain.Contracts;

public sealed record PipelineStageSetting(LeadStage Stage, string Name, int SortOrder, bool IsActive);

public sealed record PipelineStageConfigVersion(
    int Version,
    DateTime ChangedAtUtc,
    string ChangedBy,
    IReadOnlyList<PipelineStageSetting> Stages);

public sealed record PipelineStageConfigHistory(IReadOnlyList<PipelineStageConfigVersion> History);

public sealed record PipelineStageConfigSnapshot(
    PipelineStageConfigVersion? Current,
    IReadOnlyList<PipelineStageConfigVersion> History);

public sealed record PipelineStageSaveRequest(
    int ExpectedVersion,
    IReadOnlyList<PipelineStageSetting> Stages);

public sealed record PipelineStageAdminInfo(
    LeadStage Stage,
    string Name,
    bool IsActive,
    int SortOrder,
    int OpenLeadCount);

public sealed record PipelineStageAdminSnapshot(
    int ExpectedVersion,
    IReadOnlyList<PipelineStageAdminInfo> Stages);

public static class PipelineStageDefinition
{
    public static IReadOnlyList<LeadStage> ConfigurableStages { get; } = Array.AsReadOnly(
    [
        LeadStage.Contacted,
        LeadStage.Viewing,
        LeadStage.Negotiation,
        LeadStage.Custom1,
        LeadStage.Custom2,
        LeadStage.Custom3,
        LeadStage.Custom4
    ]);

    public static IReadOnlyList<PipelineStageSetting> Defaults { get; } = Array.AsReadOnly<PipelineStageSetting>(
    [
        new(LeadStage.Contacted, "Contacted", 0, true),
        new(LeadStage.Viewing, "Viewing", 1, true),
        new(LeadStage.Negotiation, "Negotiation", 2, true),
        new(LeadStage.Custom1, "Stage 1", 3, false),
        new(LeadStage.Custom2, "Stage 2", 4, false),
        new(LeadStage.Custom3, "Stage 3", 5, false),
        new(LeadStage.Custom4, "Stage 4", 6, false)
    ]);

    public static string? Validate(IReadOnlyList<PipelineStageSetting>? stages)
    {
        if (stages is null || stages.Count != ConfigurableStages.Count)
            return "The pipeline configuration must contain all seven configurable stages.";

        if (stages.Any(stage => stage is null)
            || stages.Any(stage => !ConfigurableStages.Contains(stage.Stage))
            || stages.Select(stage => stage.Stage).Distinct().Count() != ConfigurableStages.Count)
            return "The pipeline configuration contains a missing or duplicate stage.";

        if (stages.Any(stage => stage.SortOrder < 0 || stage.SortOrder >= ConfigurableStages.Count)
            || stages.Select(stage => stage.SortOrder).Distinct().Count() != ConfigurableStages.Count)
            return "Each configurable stage must have a unique position from first to seventh.";

        var names = stages.Select(stage => stage.Name?.Trim() ?? string.Empty).ToList();
        if (names.Any(name => name.Length is < 1 or > 30))
            return "Each stage name must contain 1 to 30 characters.";
        if (names.Concat(["New", "Closed", "Lost"])
            .Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Count + 3)
            return "Configurable stage names must be unique, ignoring case.";
        if (!stages.Any(stage => stage.IsActive))
            return "At least one configurable stage must remain active.";

        return null;
    }

    public static IReadOnlyList<PipelineStageInfo> GetStages(IReadOnlyList<PipelineStageSetting> configuration)
    {
        var stages = new List<PipelineStageInfo>
        {
            new(Key(LeadStage.New), "New", 0, true)
        };
        stages.AddRange(configuration.Where(stage => stage.IsActive)
            .OrderBy(stage => stage.SortOrder)
            .Select((stage, index) => new PipelineStageInfo(
                Key(stage.Stage), stage.Name, index + 1, false)));
        stages.Add(new PipelineStageInfo(Key(LeadStage.Closed), "Closed", stages.Count, true));
        stages.Add(new PipelineStageInfo(Key(LeadStage.Lost), "Lost", stages.Count, true));
        return stages;
    }

    public static IReadOnlyList<PipelineStageAdminInfo> GetAllForAdmin(
        IReadOnlyList<PipelineStageSetting> configuration,
        IReadOnlyDictionary<LeadStage, int> openLeadCounts) =>
        configuration.OrderBy(stage => stage.SortOrder)
            .Select(stage => new PipelineStageAdminInfo(
                stage.Stage,
                stage.Name,
                stage.IsActive,
                stage.SortOrder,
                openLeadCounts.TryGetValue(stage.Stage, out var count) ? count : 0))
            .ToList();

    public static LeadStage ClosingStage(IReadOnlyList<PipelineStageSetting> configuration) =>
        configuration.Where(stage => stage.IsActive).OrderBy(stage => stage.SortOrder).LastOrDefault()?.Stage
        ?? throw new InvalidOperationException("At least one configurable stage must be active.");

    public static bool IsClosingStage(
        LeadStage stage,
        IReadOnlyList<PipelineStageSetting> configuration) =>
        stage == ClosingStage(configuration);

    public static LeadStage? Next(
        LeadStage stage,
        IReadOnlyList<PipelineStageSetting> configuration)
    {
        var activeStages = configuration.Where(item => item.IsActive)
            .OrderBy(item => item.SortOrder)
            .Select(item => item.Stage)
            .ToList();
        if (activeStages.Count == 0)
            return null;
        if (stage == LeadStage.New)
            return activeStages[0];
        if (stage is LeadStage.Closed or LeadStage.Lost)
            return null;

        var currentIndex = activeStages.IndexOf(stage);
        if (currentIndex < 0)
            return null;
        return currentIndex == activeStages.Count - 1
            ? LeadStage.Closed
            : activeStages[currentIndex + 1];
    }

    public static string GetDisplayName(LeadStage stage, IReadOnlyList<PipelineStageSetting> configuration) =>
        stage switch
        {
            LeadStage.New => "New",
            LeadStage.Closed => "Closed",
            LeadStage.Lost => "Lost",
            _ => configuration.FirstOrDefault(item => item.Stage == stage)?.Name ?? "Unknown stage"
        };

    public static string Key(LeadStage stage) => stage switch
    {
        LeadStage.New => "New",
        LeadStage.Contacted => "Contacted",
        LeadStage.Viewing => "Viewing",
        LeadStage.Negotiation => "Negotiation",
        LeadStage.Closed => "Closed",
        LeadStage.Lost => "Lost",
        LeadStage.Custom1 => "Custom1",
        LeadStage.Custom2 => "Custom2",
        LeadStage.Custom3 => "Custom3",
        LeadStage.Custom4 => "Custom4",
        _ => "Unknown"
    };
}

public static class PipelineStageConfigJson
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static PipelineStageConfigHistory? Deserialize(string json) =>
        JsonSerializer.Deserialize<PipelineStageConfigHistory>(json, Options);

    public static string Serialize(PipelineStageConfigHistory history) =>
        JsonSerializer.Serialize(history, Options);
}
