namespace Halisaha.Domain.Common;

public enum UserStatus { Active, Disabled }
public enum BusinessStatus { Draft, PendingApproval, Approved, Rejected, Suspended }
public enum BusinessMemberRole { BusinessOwner, BusinessStaff }
public enum PublishStatus { Draft, Published, Archived }
public enum FieldBlockStatus { Active, Cancelled }
public enum ReservationStatus { PendingPayment, Confirmed, Completed, NoShow, Expired, CancelledByCustomer, CancelledByBusiness }
public enum PaymentStatus { Created, Pending, Authorized, Captured, Failed, Cancelled }
public enum RefundStatus { RefundPending, Refunded, RefundFailed }
public enum ProcessingStatus { Pending, Processed, Failed }
public enum NotificationChannel { Email, Sms }
public enum DeliveryStatus { Pending, Sent, Failed, DeadLetter }
