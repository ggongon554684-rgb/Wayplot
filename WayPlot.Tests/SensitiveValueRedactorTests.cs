using WayPlot.domain.Common;
using Xunit;

namespace WayPlot.Tests;

public sealed class SensitiveValueRedactorTests
{
    [Theory]
    [InlineData("Password=secret", "Password=[REDACTED]")]
    [InlineData("pwd='secret;value'", "pwd=[REDACTED]")]
    [InlineData("User ID=staff", "User ID=[REDACTED]")]
    [InlineData("uid=staff", "uid=[REDACTED]")]
    [InlineData("Token: token=abc123", "Token: token=[REDACTED]")]
    [InlineData("apiKey=key-value", "apiKey=[REDACTED]")]
    public void Redact_masks_sensitive_values_case_insensitively(string text, string expected)
    {
        Assert.Equal(expected, SensitiveValueRedactor.Redact(text));
    }

    [Fact]
    public void Redact_keeps_exception_context_and_stack_text()
    {
        const string text = "SqlException: Login failed for user id=staff. at Service.Run()";

        var result = SensitiveValueRedactor.Redact(text);

        Assert.Equal("SqlException: Login failed for user id=[REDACTED]. at Service.Run()", result);
    }

    [Fact]
    public void Redact_removes_connection_strings_without_removing_stack_text()
    {
        const string text = "SqlException: failed Server=sql;Database=tenant;Password=secret; at Service.Run()";

        var result = SensitiveValueRedactor.Redact(text);

        Assert.Equal("SqlException: failed [connection string redacted]; at Service.Run()", result);
    }
}
