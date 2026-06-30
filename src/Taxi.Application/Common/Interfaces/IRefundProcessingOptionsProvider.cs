namespace Taxi.Application.Common.Interfaces;

public interface IRefundProcessingOptionsProvider
{
    RefundProcessingOptions GetOptions();
}

public sealed record RefundProcessingOptions(bool ForceRefundFailure);
