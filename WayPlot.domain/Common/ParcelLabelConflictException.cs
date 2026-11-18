namespace WayPlot.domain.Common;

public sealed class ParcelLabelConflictException(Exception innerException)
    : Exception("The parcel label is already in use.", innerException);
