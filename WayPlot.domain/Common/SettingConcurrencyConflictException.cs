namespace WayPlot.domain.Common;

public sealed class SettingConcurrencyConflictException(Exception innerException)
    : Exception("A tenant setting changed while it was being saved.", innerException);

public sealed class StaffConcurrencyConflictException(Exception innerException)
    : Exception("A staff account changed while it was being saved.", innerException);
