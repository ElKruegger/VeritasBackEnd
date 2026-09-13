namespace LuxuryStore.Application.Exceptions;

/// <summary>
/// Thrown when a requested entity does not exist. The API's global
/// exception handler (see LuxuryStore-Backend/Middleware/GlobalExceptionHandler.cs)
/// maps this to a 404 response, so services can just throw instead of every
/// controller action having to check for null and return NotFound() itself.
/// </summary>
public class NotFoundException(string message) : Exception(message);

/// <summary>
/// Thrown for a business-rule violation such as a duplicate slug. Mapped to
/// a 409 response by the API's global exception handler.
/// </summary>
public class ConflictException(string message) : Exception(message);

/// <summary>
/// Thrown for a failed authentication attempt (bad credentials, invalid or
/// expired refresh token) or a deactivated account. Mapped to a 401
/// response by the API's global exception handler. Deliberately generic
/// messages at the call site (AuthService) — "invalid email or password",
/// never "no account with that email" — so this never becomes a way to
/// enumerate which emails are registered.
/// </summary>
public class UnauthorizedException(string message) : Exception(message);
