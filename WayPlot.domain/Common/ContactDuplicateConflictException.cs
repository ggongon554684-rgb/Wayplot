namespace WayPlot.domain.Common;

public sealed class ContactDuplicateConflictException(Exception innerException)
    : Exception("A contact with the same phone number or email already exists.", innerException);
