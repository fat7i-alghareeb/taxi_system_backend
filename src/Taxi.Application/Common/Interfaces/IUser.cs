namespace Taxi.Application.Common.Interfaces;

public interface IUser
{
    string? Id { get; }

    bool IsAdmin { get; }
}

