using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using Xunit;

namespace WayPlot.Tests;

public sealed class PipelineStageConfigurationTests
{
    [Fact]
    public void Defaults_keep_the_existing_six_visible_stages_and_inactive_custom_slots()
    {
        var stages = PipelineStageDefinition.GetStages(PipelineStageDefinition.Defaults);

        Assert.Equal(
            ["New", "Contacted", "Viewing", "Negotiation", "Closed", "Lost"],
            stages.Select(stage => stage.Key));
        Assert.Equal("Stage 1", PipelineStageDefinition.GetDisplayName(LeadStage.Custom1, PipelineStageDefinition.Defaults));
    }

    [Fact]
    public void Provider_order_uses_active_stage_positions_between_fixed_stages()
    {
        var configuration = new[]
        {
            new PipelineStageSetting(LeadStage.Contacted, "Call", 3, false),
            new PipelineStageSetting(LeadStage.Viewing, "Visit", 1, true),
            new PipelineStageSetting(LeadStage.Negotiation, "Offer", 2, true),
            new PipelineStageSetting(LeadStage.Custom1, "Site visit 2", 0, true),
            new PipelineStageSetting(LeadStage.Custom2, "Stage 2", 4, false),
            new PipelineStageSetting(LeadStage.Custom3, "Stage 3", 5, false),
            new PipelineStageSetting(LeadStage.Custom4, "Stage 4", 6, false)
        };

        var stages = PipelineStageDefinition.GetStages(configuration);

        Assert.Equal(
            ["New", "Custom1", "Viewing", "Negotiation", "Closed", "Lost"],
            stages.Select(stage => stage.Key));
        Assert.Equal("Site visit 2", stages[1].Name);
        Assert.Equal(LeadStage.Custom1, PipelineStageDefinition.Next(LeadStage.New, configuration));
        Assert.Equal(LeadStage.Viewing, PipelineStageDefinition.Next(LeadStage.Custom1, configuration));
        Assert.Equal(LeadStage.Closed, PipelineStageDefinition.Next(LeadStage.Negotiation, configuration));
    }

    [Fact]
    public void Closing_stage_is_last_active_configurable_stage()
    {
        var configuration = PipelineStageDefinition.Defaults
            .Select(stage => stage with
            {
                IsActive = stage.IsActive && stage.Stage != LeadStage.Negotiation
            })
            .ToList();

        Assert.Equal(LeadStage.Viewing, PipelineStageDefinition.ClosingStage(configuration));
        Assert.True(PipelineStageDefinition.IsClosingStage(LeadStage.Viewing, configuration));
        Assert.False(PipelineStageDefinition.IsClosingStage(LeadStage.Contacted, configuration));
    }

    [Fact]
    public void Names_must_be_unique_ignoring_case_and_have_one_to_thirty_characters()
    {
        var duplicateNames = PipelineStageDefinition.Defaults.ToList();
        duplicateNames[0] = duplicateNames[0] with { Name = "  Stage 1 " };
        duplicateNames[3] = duplicateNames[3] with { Name = "stage 1" };

        Assert.Contains("unique", PipelineStageDefinition.Validate(duplicateNames), StringComparison.OrdinalIgnoreCase);

        var invalidLength = PipelineStageDefinition.Defaults.ToList();
        invalidLength[0] = invalidLength[0] with { Name = new string('x', 31) };

        Assert.Contains("1 to 30", PipelineStageDefinition.Validate(invalidLength), StringComparison.Ordinal);

        var fixedNameCollision = PipelineStageDefinition.Defaults.ToList();
        fixedNameCollision[0] = fixedNameCollision[0] with { Name = "Closed" };
        Assert.Contains("unique", PipelineStageDefinition.Validate(fixedNameCollision), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void At_least_one_configurable_stage_must_remain_active()
    {
        var configuration = PipelineStageDefinition.Defaults
            .Select(stage => stage with { IsActive = false })
            .ToList();

        Assert.Contains("At least one", PipelineStageDefinition.Validate(configuration), StringComparison.Ordinal);
    }

    [Fact]
    public void Configuration_json_writes_stage_keys_as_text_and_reads_numeric_values()
    {
        var version = new PipelineStageConfigVersion(
            1,
            DateTime.UnixEpoch,
            "Admin",
            [new PipelineStageSetting(LeadStage.Custom1, "Site visit 2", 0, true)]);
        var json = PipelineStageConfigJson.Serialize(new PipelineStageConfigHistory([version]));

        Assert.Contains("\"Stage\":\"Custom1\"", json, StringComparison.Ordinal);

        const string legacy = """{"History":[{"Version":1,"ChangedAtUtc":"1970-01-01T00:00:00Z","ChangedBy":"Admin","Stages":[{"Stage":6,"Name":"Site visit 2","SortOrder":0,"IsActive":true}]}]}""";
        var loaded = PipelineStageConfigJson.Deserialize(legacy);

        Assert.Equal(LeadStage.Custom1, loaded?.History[0].Stages[0].Stage);
    }
}
