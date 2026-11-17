namespace WayPlot.domain.Common;

public sealed class ReceiptNumberConflictException(Exception innerException)
    : Exception("The receipt number was already used.", innerException);
