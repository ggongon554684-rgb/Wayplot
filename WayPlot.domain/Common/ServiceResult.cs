namespace WayPlot.domain.Common;

/// <summary>Like RuleResult, but carries a value on success. Blocked cases return Fail with a friendly message instead of throwing.</summary>
public readonly record struct ServiceResult<T>(bool Ok, string Message, T? Value)
{
    public static ServiceResult<T> Success(T value, string message = "") => new(true, message, value);
    public static ServiceResult<T> Fail(string message) => new(false, message, default);
}