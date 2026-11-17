namespace WayPlot.domain.Services.DataCollection;

/// <summary>Philippine mobile numbers. 0917 123 4567, +63 917 123 4567 and 917 123 4567 are the same number.</summary>
public static class PhoneNormalizer
{
    public static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        var digits = new string(input.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("63")) digits = "0" + digits[2..];
        else if (digits.Length == 10 && digits[0] == '9') digits = "0" + digits;

        return digits.Length == 11 && digits.StartsWith("09") ? digits : null;
    }
}
