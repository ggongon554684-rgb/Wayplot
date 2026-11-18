using System.Reflection;
using System.Text.Json;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Entities;
using WayPlot.domain.Services.MainTransaction;
using Xunit;

namespace WayPlot.Tests;

public sealed class CommissionRulesSerializationTests
{
    [Fact]
    public void Numeric_enum_values_in_existing_rules_json_still_load()
    {
        const string value =
            """{"History":[{"Version":3,"ChangedAtUtc":"2026-11-18T00:00:00Z","ChangedBy":"Admin","CommissionType":1,"CommissionAmount":5.0,"AgentSharePercent":50.0,"CompanySharePercent":50.0,"BranchManagerSharePercent":0.0,"ReferralType":0,"ReferralAmount":100.0,"PayableTiming":1}]}""";
        var method = typeof(CommissionService).GetMethod("TryReadRules", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("Commission rules reader was not found.");
        object?[] arguments = [new Setting { Key = "deals.commission-rules", Value = value }, null];

        var succeeded = (bool)(method.Invoke(null, arguments) ?? false);
        var snapshot = arguments[1] as CommissionRulesSnapshot;

        Assert.True(succeeded);
        Assert.NotNull(snapshot?.Current);
        Assert.Equal(CommissionCalculationType.Percent, snapshot.Current!.CommissionType);
        Assert.Equal(CommissionCalculationType.Fixed, snapshot.Current.ReferralType);
        Assert.Equal(CommissionPayableTiming.AfterDownPayment, snapshot.Current.PayableTiming);
    }

    [Fact]
    public void New_rules_json_writes_calculation_and_timing_enums_as_text()
    {
        var options = typeof(CommissionService).GetField("RuleJsonOptions", BindingFlags.NonPublic | BindingFlags.Static)
            ?.GetValue(null) as JsonSerializerOptions
            ?? throw new InvalidOperationException("Commission rules JSON options were not found.");
        var version = new CommissionRulesVersion(
            1, DateTime.UnixEpoch, "Admin", CommissionCalculationType.Percent, 5m,
            50m, 50m, 0m, CommissionCalculationType.Fixed, 100m,
            CommissionPayableTiming.AfterDownPayment);

        var json = JsonSerializer.Serialize(new { History = new[] { version } }, options);

        Assert.Contains("\"CommissionType\":\"Percent\"", json, StringComparison.Ordinal);
        Assert.Contains("\"ReferralType\":\"Fixed\"", json, StringComparison.Ordinal);
        Assert.Contains("\"PayableTiming\":\"AfterDownPayment\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Agent_override_wins_when_agent_and_parcel_overrides_both_match()
    {
        var agentId = Guid.NewGuid();
        var parcelId = Guid.NewGuid();
        var expected = new CommissionRuleOverride(CommissionOverrideScope.Agent, agentId,
            CommissionCalculationType.Fixed, 250m);
        var rules = new CommissionRulesVersion(
            1, DateTime.UnixEpoch, "Admin", CommissionCalculationType.Percent, 5m,
            50m, 50m, 0m, CommissionCalculationType.Fixed, 100m,
            CommissionPayableTiming.AfterDownPayment,
            [
                new CommissionRuleOverride(CommissionOverrideScope.Parcel, parcelId, CommissionCalculationType.Percent, 7m),
                expected
            ]);
        var method = typeof(CommissionService).GetMethod("ResolveCommissionOverride", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("Commission override resolver was not found.");

        var actual = method.Invoke(null, [rules, agentId, parcelId]) as CommissionRuleOverride;

        Assert.Equal(expected, actual);
    }
}
