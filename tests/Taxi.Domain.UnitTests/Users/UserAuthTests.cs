using Taxi.Domain.Users;

using Xunit;

namespace Taxi.Domain.UnitTests.Users;

public class UserAuthTests
{
    private static readonly Guid Id = Guid.NewGuid();

    [Fact]
    public void Create_PhoneFirst_DefaultsToPhoneVerified()
    {
        var result = User.Create(Id, "Passenger +31612345678", "+31612345678", null, UserRole.Passenger);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsPhoneVerified);
        Assert.False(result.Value.IsEmailVerified);
    }

    [Fact]
    public void Create_GoogleEmailSignup_StoresUnverifiedPhoneAndVerifiedEmail()
    {
        var result = User.Create(
            Id,
            "Ada",
            "+31612345678",
            "ada@example.com",
            UserRole.Passenger,
            isPhoneVerified: false,
            isEmailVerified: true,
            googleId: "google-uid-123");

        Assert.True(result.IsSuccess);
        var user = result.Value;
        Assert.False(user.IsPhoneVerified);
        Assert.True(user.IsEmailVerified);
        Assert.Equal("google-uid-123", user.GoogleId);
        Assert.Equal("ada@example.com", user.Email);
    }

    [Fact]
    public void MarkPhoneVerified_SetsFlag()
    {
        var user = User.Create(Id, "Ada", "+31612345678", null, UserRole.Passenger, isPhoneVerified: false).Value;

        var result = user.MarkPhoneVerified();

        Assert.True(result.IsSuccess);
        Assert.True(user.IsPhoneVerified);
    }

    [Fact]
    public void SetVerifiedPhone_ChangesPhoneAndVerifies()
    {
        var user = User.Create(Id, "Ada", "+31600000000", null, UserRole.Passenger, isPhoneVerified: false).Value;

        var result = user.SetVerifiedPhone("+31612345678");

        Assert.True(result.IsSuccess);
        Assert.Equal("+31612345678", user.Phone);
        Assert.True(user.IsPhoneVerified);
    }

    [Fact]
    public void MarkEmailVerified_WithoutEmail_Fails()
    {
        var user = User.Create(Id, "Ada", "+31612345678", null, UserRole.Passenger).Value;

        var result = user.MarkEmailVerified();

        Assert.True(result.IsError);
        Assert.False(user.IsEmailVerified);
    }

    [Fact]
    public void ResetForFreshStart_WipesProfile_KeepsVerifiedPhone_StampsReset()
    {
        var user = User.Create(Id, "Ada", "+31612345678", "ada@example.com", UserRole.Passenger, isEmailVerified: true).Value;
        var now = DateTimeOffset.UtcNow;

        var result = user.ResetForFreshStart("Passenger +31612345678", now);

        Assert.True(result.IsSuccess);
        Assert.Null(user.Email);
        Assert.False(user.IsEmailVerified);
        Assert.Equal(now, user.ProfileResetAtUtc);
        // Phone and its verified status are preserved (identity is retained).
        Assert.Equal("+31612345678", user.Phone);
        Assert.True(user.IsPhoneVerified);
    }

    [Fact]
    public void UpdateProfile_ChangingEmail_UnverifiesIt()
    {
        var user = User.Create(Id, "Ada", "+31612345678", "ada@example.com", UserRole.Passenger, isEmailVerified: true).Value;

        user.UpdateProfile("Ada Lovelace", email: "new@example.com");

        Assert.Equal("new@example.com", user.Email);
        Assert.False(user.IsEmailVerified);
    }
}
