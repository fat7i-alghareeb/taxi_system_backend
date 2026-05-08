namespace Taxi.Domain.Auth;

public sealed class OtpSession
{
    public OtpSession(Guid id, string phone, DateTime expiresAtUtc)
    {
        Id = id;
        Phone = phone;
        Attempts = 0;
        Consumed = false;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = DateTime.UtcNow;
    }

    private OtpSession() { }

    public Guid Id { get; private set; }
    public string Phone { get; private set; } = default!;
    public int Attempts { get; private set; }
    public bool Consumed { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public void RecordAttempt() => Attempts++;
    public void Consume() => Consumed = true;
    public bool IsExpired() => DateTime.UtcNow > ExpiresAtUtc;
}

