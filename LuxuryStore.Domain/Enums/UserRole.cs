namespace LuxuryStore.Domain.Enums;

/// <summary>
/// Authorization role of a User. Persisted as its string name (see
/// UserConfiguration), not as an int, so a row read in Supabase Studio or
/// DBeaver says "Admin" instead of "1", and reordering/adding members here
/// can never silently change what an existing row means.
///
/// v1 has no endpoint that grants Admin — on purpose. Promoting an account
/// is an operator action done directly in the database (README: "Granting
/// admin access"), which keeps privilege escalation off the public API
/// surface entirely. The new role reaches the JWT on the user's next login
/// or refresh (at most Jwt:AccessTokenMinutes later).
/// </summary>
public enum UserRole
{
    Customer = 0,
    Admin = 1
}
