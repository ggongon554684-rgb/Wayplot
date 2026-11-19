using WayPlot.domain.Common;
using Xunit;

namespace WayPlot.Tests;

public sealed class ContactDuplicateConflictDetectorTests
{
    [Theory]
    [InlineData(2601, "Cannot insert duplicate key row UX_Contact_Phone.")]
    [InlineData(2627, "Violation of UNIQUE KEY constraint 'UX_Contact_Email'.")]
    public void DetectsContactUniqueIndexViolations(int number, string message)
    {
        Assert.True(ContactDuplicateConflictDetector.IsContactUniqueViolation(number, message));
    }

    [Fact]
    public void IgnoresOtherConstraintNames()
    {
        Assert.False(ContactDuplicateConflictDetector.IsContactUniqueViolation(
            2601, "Cannot insert duplicate key row UX_Payment_Receipt."));
    }

    [Fact]
    public void IgnoresNonUniqueConstraintErrors()
    {
        Assert.False(ContactDuplicateConflictDetector.IsContactUniqueViolation(
            547, "UX_Contact_Email foreign key violation."));
    }
}
