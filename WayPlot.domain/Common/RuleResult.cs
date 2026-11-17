namespace WayPlot.domain.Common;

/// <summary>Outcome of a business rule. Services return this instead of throwing for normal "blocked" cases.</summary>
public readonly record struct RuleResult(bool Ok, string Message)
{
    public static RuleResult Success(string message = "") => new(true, message);
    public static RuleResult Fail(string message) => new(false, message);
}
