namespace WayPlot.domain.Common;

public sealed class DealLifecycleConcurrencyConflictException(Exception innerException)
    : Exception("A deal, payment, contact, or payout changed while it was being saved.", innerException);
