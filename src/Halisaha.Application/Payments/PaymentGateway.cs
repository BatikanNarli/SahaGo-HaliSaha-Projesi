namespace Halisaha.Application.Payments;

public sealed record PaymentCaptureResult(
    string Provider,
    string ProviderPaymentId,
    DateTime CapturedAtUtc);

public sealed record PaymentRefundResult(
    string ProviderRefundId,
    DateTime RefundedAtUtc);

public interface IPaymentGateway
{
    Task<PaymentCaptureResult> CaptureAsync(
        Guid reservationId,
        decimal amount,
        string currency,
        CancellationToken cancellationToken);

    Task<PaymentRefundResult> RefundAsync(
        Guid paymentId,
        Guid reservationId,
        decimal amount,
        string currency,
        CancellationToken cancellationToken);
}
