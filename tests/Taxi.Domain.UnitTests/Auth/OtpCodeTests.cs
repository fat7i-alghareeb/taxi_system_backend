using Taxi.Contracts.Common;
using Taxi.Domain.Auth;

using Xunit;

namespace Taxi.Domain.UnitTests.Auth;

public class OtpCodeTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 5, 12, 0, 0, TimeSpan.Zero);

    private static OtpCode Create(string codeHash = "hash-a", int maxAttempts = 3, TimeSpan? lifetime = null)
        => OtpCode.Create(
            OtpChannel.Sms,
            OtpPurpose.PhoneLogin,
            "+31612345678",
            codeHash,
            Now,
            lifetime ?? TimeSpan.FromMinutes(5),
            maxAttempts);

    [Fact]
    public void Verify_WithCorrectHash_Succeeds_AndConsumes()
    {
        var otp = Create();

        var result = otp.Verify("hash-a", Now.AddMinutes(1));

        Assert.True(result.IsSuccess);
        Assert.True(otp.IsConsumed);
    }

    [Fact]
    public void Verify_AfterConsumed_ReturnsAlreadyUsed()
    {
        var otp = Create();
        otp.Verify("hash-a", Now.AddMinutes(1));

        var result = otp.Verify("hash-a", Now.AddMinutes(1));

        Assert.True(result.IsError);
        Assert.Equal(LocalizationKeys.Otp.AlreadyUsed, result.Error.Code);
    }

    [Fact]
    public void Verify_AfterExpiry_ReturnsExpired()
    {
        var otp = Create(lifetime: TimeSpan.FromMinutes(5));

        var result = otp.Verify("hash-a", Now.AddMinutes(6));

        Assert.True(result.IsError);
        Assert.Equal(LocalizationKeys.Otp.Expired, result.Error.Code);
        Assert.False(otp.IsConsumed);
    }

    [Fact]
    public void Verify_WithWrongHash_IncrementsAttempts_ThenLocksAtMax()
    {
        var otp = Create(maxAttempts: 3);

        var first = otp.Verify("wrong", Now.AddMinutes(1));
        Assert.Equal(LocalizationKeys.Otp.Invalid, first.Error.Code);

        var second = otp.Verify("wrong", Now.AddMinutes(1));
        Assert.Equal(LocalizationKeys.Otp.Invalid, second.Error.Code);

        // Third wrong attempt reaches the limit.
        var third = otp.Verify("wrong", Now.AddMinutes(1));
        Assert.Equal(LocalizationKeys.Otp.MaxAttempts, third.Error.Code);

        // Even a correct code is rejected once locked.
        var afterLock = otp.Verify("hash-a", Now.AddMinutes(1));
        Assert.Equal(LocalizationKeys.Otp.MaxAttempts, afterLock.Error.Code);
        Assert.False(otp.IsConsumed);
    }

    [Fact]
    public void Id_IsStableRequestId()
    {
        var otp = Create();
        Assert.NotEqual(Guid.Empty, otp.Id);
    }
}
