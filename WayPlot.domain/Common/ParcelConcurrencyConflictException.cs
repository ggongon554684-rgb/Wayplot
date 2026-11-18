namespace WayPlot.domain.Common;

public sealed class ParcelConcurrencyConflictException(Exception innerException)
    : Exception("The parcel changed while it was being saved.", innerException);
