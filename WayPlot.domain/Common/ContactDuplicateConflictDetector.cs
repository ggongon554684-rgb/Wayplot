namespace WayPlot.domain.Common;

public static class ContactDuplicateConflictDetector
{
    public static bool IsContactUniqueViolation(int sqlErrorNumber, string? sqlMessage)
    {
        if (sqlErrorNumber is not (2601 or 2627) || string.IsNullOrEmpty(sqlMessage))
            return false;

        return sqlMessage.Contains("UX_Contact_Phone", StringComparison.OrdinalIgnoreCase)
            || sqlMessage.Contains("UX_Contact_Email", StringComparison.OrdinalIgnoreCase);
    }
}
