namespace WayPlot.domain.Common;

public enum Tier { A = 1, B = 2, C = 3 }
public enum TenantStatus { Provisioning, Active, Suspended, Failed }
public enum SubscriptionStatus { Active, Suspended, Expired }
public enum SyncStatus { Pending, Synced, Conflict }
public enum ParcelStatus { Available, Reserved, Sold, Leased }
public enum ContactType { Customer, Landowner, Client }
public enum LeadSource { Signboard, WalkIn, PhoneCall, Facebook, Referral, Other, Import }
public enum LeadStage { New, Contacted, Viewing, Negotiation, Closed, Lost }
public enum InteractionType { Inquiry, Complaint, Feedback }
public enum InteractionChannel { Call, Sms, Email, WalkIn, Viewing, Note }
public enum ComplaintStatus { Open, Resolved }
public enum DealStatus { Current, Overdue, FullyPaid, Cancelled, Defaulted }
public enum PaymentStatus { AwaitingAdmin, Confirmed, Returned }
public enum PaymentMethod { Cash, BankTransfer, GCash }
public enum CommissionStatus { Pending, Approved, Paid, Cancelled, PaidNeedsReview }
public enum CommissionCalculationType { Fixed, Percent }
public enum CommissionPayableTiming { OnConfirmation, AfterDownPayment }
public enum PayoutKind { Commission, ReferralBonus }
public enum DocumentType { Contract, DeedOfSale, Title, TaxDeclaration, BuyerId, Receipt }
