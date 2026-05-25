using Microsoft.AspNetCore.Identity;

namespace Taxi.Infrastructure.Identity;

public class AppUser : IdentityUser
{
    public bool RequiresPasswordReset { get; set; } = false;
}

