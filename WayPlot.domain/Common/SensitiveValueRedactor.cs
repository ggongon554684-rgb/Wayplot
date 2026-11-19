using System.Text.RegularExpressions;

namespace WayPlot.domain.Common;

public static partial class SensitiveValueRedactor
{
    [GeneratedRegex(
        @"\b(?:data\s+source|server|address|network\s+address|host)\s*=\s*(?:""[^""]*""|'[^']*'|[^;\r\n]+)(?:\s*;\s*[^;\r\n=]+\s*=\s*(?:""[^""]*""|'[^']*'|[^;\r\n]*))+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ConnectionStringRegex();

    [GeneratedRegex(
        @"(?<prefix>\b(?:password|pwd|user\s+id|uid|token|apikey)\s*=\s*)(?:""[^""]*""|'[^']*'|(?:(?!\.(?:\s|$))[^;\s,])+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CredentialValueRegex();

    public static string Redact(string text)
    {
        var withoutConnectionStrings = ConnectionStringRegex().Replace(text, "[connection string redacted]");
        return CredentialValueRegex().Replace(withoutConnectionStrings, "${prefix}[REDACTED]");
    }
}
