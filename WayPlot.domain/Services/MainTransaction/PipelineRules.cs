using WayPlot.domain.Common;
using WayPlot.domain.Entities;

namespace WayPlot.domain.Services.MainTransaction;

/// <summary>Pipeline rules. Forms and presenters ask these rules; they never decide on their own.</summary>
public static class PipelineRules
{
    public static LeadStage? Next(LeadStage stage) => stage switch
    {
        LeadStage.New => LeadStage.Contacted,
        LeadStage.Contacted => LeadStage.Viewing,
        LeadStage.Viewing => LeadStage.Negotiation,
        LeadStage.Negotiation => LeadStage.Closed,
        _ => null
    };

    public static RuleResult CanMove(Lead lead, LeadStage to, bool anotherLeadHoldsParcel)
    {
        if (lead.Stage is LeadStage.Closed or LeadStage.Lost)
            return RuleResult.Fail("This lead is already finished.");

        if (to == LeadStage.Lost)
            return RuleResult.Success();

        if (to == LeadStage.Closed)
            return RuleResult.Fail("The agent requests Closed and the Tenant Admin confirms it.");

        if (Next(lead.Stage) != to)
            return RuleResult.Fail($"A lead moves one step at a time. The next stage is {Next(lead.Stage)}.");

        if (to == LeadStage.Negotiation && anotherLeadHoldsParcel)
            return RuleResult.Fail("Another lead is already in Negotiation on this parcel.");

        return RuleResult.Success();
    }

    public static RuleResult CanConfirmDeal(Lead lead)
    {
        if (lead.Stage is LeadStage.Closed or LeadStage.Lost)
            return RuleResult.Fail("This lead is already finished.");

        if (lead.Stage != LeadStage.Negotiation)
            return RuleResult.Fail("Only a lead in Negotiation can be closed by confirming a deal.");

        if (!lead.CloseRequested)
            return RuleResult.Fail("The agent has not requested that this lead be closed.");

        return RuleResult.Success();
    }
}
