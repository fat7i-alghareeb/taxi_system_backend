using Taxi.Domain.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Promos;

public sealed class PromoCode : AuditableEntity
{
    private PromoCode() { }

    private PromoCode(
        Guid id,
        string code,
        decimal discountAmount,
        decimal? discountPercentage,
        DateTime expiresAtUtc,
        int? maxUsageCount)
        : base(id)
    {
        Code = code.ToUpper();
        DiscountAmount = discountAmount;
        DiscountPercentage = discountPercentage;
        ExpiresAtUtc = expiresAtUtc;
        MaxUsageCount = maxUsageCount;
        UsageCount = 0;
        IsActive = true;
    }

    public string Code { get; private set; } = default!;
    public decimal DiscountAmount { get; private set; }
    public decimal? DiscountPercentage { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public int? MaxUsageCount { get; private set; }
    public int UsageCount { get; private set; }
    public bool IsActive { get; private set; }

    public static Result<PromoCode> Create(
        Guid id,
        string code,
        decimal discountAmount,
        decimal? discountPercentage,
        DateTime expiresAtUtc,
        int? maxUsageCount)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return PromoErrors.CodeRequired;
        }

        return new PromoCode(id, code, discountAmount, discountPercentage, expiresAtUtc, maxUsageCount);
    }

    public bool IsValid()
    {
        if (!IsActive)
        {
            return false;
        }

        if (DateTime.UtcNow > ExpiresAtUtc)
        {
            return false;
        }

        if (MaxUsageCount.HasValue && UsageCount >= MaxUsageCount.Value)
        {
            return false;
        }

        return true;
    }

    public void RecordUsage()
    {
        UsageCount++;
    }

    public void Deactivate() => IsActive = false;
}

