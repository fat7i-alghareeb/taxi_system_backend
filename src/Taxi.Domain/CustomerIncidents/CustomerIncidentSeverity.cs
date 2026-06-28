namespace Taxi.Domain.CustomerIncidents;

public enum CustomerIncidentSeverity
{
    /// <summary>Notable, no action usually required (e.g. passenger self-cancel, refund issued).</summary>
    Info = 0,

    /// <summary>Should be looked at (e.g. driver cancellation, low rating, late-driver claim).</summary>
    Warning = 1,

    /// <summary>Needs prompt attention (e.g. payment failure). Also pushed to admins.</summary>
    Critical = 2,
}
