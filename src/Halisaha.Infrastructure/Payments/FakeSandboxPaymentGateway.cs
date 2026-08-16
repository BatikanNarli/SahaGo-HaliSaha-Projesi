using Halisaha.Application.Payments;

namespace Halisaha.Infrastructure.Payments;

internal sealed class FakeSandboxPaymentGateway(TimeProvider timeProvider) : IPaymentGateway
{
    public Task<PaymentCaptureResult> CaptureAsync(
        Guid reservationId,
        decimal amount,
        string currency,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new PaymentCaptureResult(
            "FakeSandbox",
            $"fake_{reservationId:N}",
            timeProvider.GetUtcNow().UtcDateTime));
    }

    public Task<PaymentRefundResult> RefundAsync(
        Guid paymentId,
        Guid reservationId,
        decimal amount,
        string currency,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new PaymentRefundResult(
            $"fake_refund_{reservationId:N}",
            timeProvider.GetUtcNow().UtcDateTime));
    }
}
