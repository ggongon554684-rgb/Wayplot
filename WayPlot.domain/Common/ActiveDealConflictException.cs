namespace WayPlot.domain.Common;

public sealed class ActiveDealConflictException(Exception innerException)
    : Exception("Another active deal already holds this parcel.", innerException);
