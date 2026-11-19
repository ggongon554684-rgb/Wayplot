using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Entities;

namespace WayPlot.domain.Services.MainTransaction;

/// <summary>Pipeline rules. Forms and presenters ask these rules; they never decide on their own.</summary>
public static class PipelineRules
{
    public static LeadStage? Next(LeadStage stage, IReadOnlyList<PipelineStageSetting> configuration) =>
        PipelineStageDefinition.Next(stage, configuration);

    public static RuleResult CanMove(
        Lead lead,
        LeadStage to,
        LeadStage? nextStage,
        bool anotherLeadHoldsParcel,
        string? nextStageName,
        string negotiationStageName)
    {
        if (lead.Stage is LeadStage.Closed or LeadStage.Lost)
            return RuleResult.Fail("This lead is already finished.");

        if (to == LeadStage.Lost)
            return RuleResult.Success();

        if (to == LeadStage.Closed)
            return RuleResult.Fail("The agent requests Closed and the Tenant Admin confirms it.");

        if (nextStage != to)
            return RuleResult.Fail(nextStage is not null
                ? $"A lead moves one step at a time. The next stage is {nextStageName}."
                : "This lead has no next active stage.");

        if (to == LeadStage.Negotiation && anotherLeadHoldsParcel)
            return RuleResult.Fail($"Another lead is already in {negotiationStageName} on this parcel.");

        return RuleResult.Success();
    }

    public static RuleResult CanConfirmDeal(Lead lead, LeadStage closingStage, string closingStageName)
    {
        if (lead.Stage is LeadStage.Closed or LeadStage.Lost)
            return RuleResult.Fail("This lead is already finished.");

        if (lead.Stage != closingStage)
            return RuleResult.Fail($"Only a lead in {closingStageName} can be closed by confirming a deal.");

        if (!lead.CloseRequested)
            return RuleResult.Fail("The agent has not requested that this lead be closed.");

        return RuleResult.Success();
    }
}
