namespace LuxuryStore_Backend.Authorization;

/// <summary>
/// Named authorization policies, registered in Program.cs. Controllers use
/// [Authorize(Policy = Policies.AdminOnly)] instead of
/// [Authorize(Roles = "Admin")] so the rule behind "admin" can change later
/// (extra claims, MFA requirement...) in one place without touching every
/// endpoint.
/// </summary>
public static class Policies
{
    public const string AdminOnly = nameof(AdminOnly);
}
