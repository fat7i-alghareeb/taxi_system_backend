using NSubstitute;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Users.Queries.GetCurrentUser;
using Taxi.Application.UnitTests.Infrastructure;
using Taxi.Domain.Admins;
using Taxi.Domain.Users;

using Xunit;

namespace Taxi.Application.UnitTests.Auth;

public class GetCurrentUserQueryHandlerVerificationFlagsTests
{
    [Fact]
    public async Task Handle_DomainUser_ReturnsRealVerificationFlags_NotAlwaysFalse()
    {
        // Distinct combination (verified email, unverified phone) so a regression that
        // hardcodes both to false, or swaps them, is caught either way.
        var user = User.Create(
            Guid.NewGuid(), "Ada", "+31612345678", "ada@example.com", UserRole.Passenger,
            isPhoneVerified: false, isEmailVerified: true).Value;

        var context = Substitute.For<IAppDbContext>();
        var adminProfiles = DbSetMockFactory.Create(new List<AdminProfile>());
        context.AdminProfiles.Returns(adminProfiles);
        var domainUsers = DbSetMockFactory.Create(new List<User> { user });
        context.DomainUsers.Returns(domainUsers);

        var identityService = Substitute.For<IIdentityService>();
        identityService.RequiresPasswordResetAsync(Arg.Any<string>()).Returns(false);

        var currentUser = Substitute.For<IUser>();
        currentUser.Id.Returns(user.Id.ToString());

        var handler = new GetCurrentUserQueryHandler(context, identityService, currentUser);

        var result = await handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsEmailVerified);
        Assert.False(result.Value.IsPhoneVerified);
    }
}
