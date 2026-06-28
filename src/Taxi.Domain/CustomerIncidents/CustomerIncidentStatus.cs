namespace Taxi.Domain.CustomerIncidents;

public enum CustomerIncidentStatus
{
    /// <summary>Newly raised, not yet triaged.</summary>
    Open = 0,

    /// <summary>An admin is actively handling it.</summary>
    InReview = 1,

    /// <summary>Closed — handled (with optional resolution note).</summary>
    Resolved = 2,

    /// <summary>Closed — no action needed / not a real problem.</summary>
    Dismissed = 3,
}
