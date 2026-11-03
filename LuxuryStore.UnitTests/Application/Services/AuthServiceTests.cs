using LuxuryStore.Application.DTOs;
using LuxuryStore.Application.Exceptions;
using LuxuryStore.Application.Interfaces;
using LuxuryStore.Application.Services;
using LuxuryStore.Domain.Entities;
using LuxuryStore.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using static LuxuryStore.UnitTests.TestHelpers;

namespace LuxuryStore.UnitTests.Application.Services;

/// <summary>
/// AuthService against fakes: repositories and ITokenService are
/// NSubstitute substitutes, the password hasher is the real
/// PasswordHasher&lt;User&gt; (fast enough, and faking it would hide real
/// hash/verify mismatches).
/// </summary>
public class AuthServiceTests
{
    private const string Password = "Veritas2026";

    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly ITokenService _tokens = Substitute.For<ITokenService>();
    private readonly PasswordHasher<User> _hasher = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _unitOfWork.Users.Returns(_users);
        _unitOfWork.RefreshTokens.Returns(_refreshTokens);

        _tokens.GenerateAccessToken(Arg.Any<User>()).Returns(("access-token", DateTime.UtcNow.AddMinutes(15)));
        _tokens.GenerateRefreshToken().Returns(("new-refresh-token", DateTime.UtcNow.AddDays(14)));
        _tokens.HashRefreshToken(Arg.Any<string>()).Returns(call => "hash:" + call.Arg<string>());

        _sut = new AuthService(_unitOfWork, _hasher, _tokens);
    }

    private User ExistingUser(bool isActive = true)
    {
        var user = new User
        {
            Email = "erick@example.com",
            FirstName = "Erick",
            LastName = "Krug",
            IsActive = isActive,
            Role = UserRole.Customer
        };
        user.PasswordHash = _hasher.HashPassword(user, Password);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _users.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        return user;
    }

    private RefreshToken StoredToken(Guid userId, string raw = "old-refresh-token",
        DateTime? expiresAt = null, DateTime? revokedAt = null)
    {
        var token = new RefreshToken
        {
            UserId = userId,
            TokenHash = "hash:" + raw,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(7),
            RevokedAt = revokedAt
        };
        _refreshTokens.GetByTokenHashAsync(token.TokenHash, Arg.Any<CancellationToken>()).Returns(token);
        return token;
    }

    // --- Register -------------------------------------------------------

    [Fact]
    public async Task Register_with_an_email_already_in_use_throws_conflict()
    {
        _users.EmailExistsAsync("erick@example.com", Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(true);

        var dto = new RegisterDto { Email = "erick@example.com", Password = Password, FirstName = "E", LastName = "K" };

        await Should.ThrowAsync<ConflictException>(() => _sut.RegisterAsync(dto, Ct));
        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Register_normalizes_email_hashes_password_and_creates_a_customer()
    {
        var dto = new RegisterDto { Email = "  Erick@Example.COM ", Password = Password, FirstName = "Erick", LastName = "Krug" };
        User? saved = null;
        _users.When(x => x.AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()))
            .Do(call => saved = call.Arg<User>());

        var result = await _sut.RegisterAsync(dto, Ct);

        saved.ShouldNotBeNull();
        saved.Email.ShouldBe("erick@example.com");
        saved.Role.ShouldBe(UserRole.Customer);
        saved.PasswordHash.ShouldNotBe(Password);
        _hasher.VerifyHashedPassword(saved, saved.PasswordHash, Password).ShouldNotBe(PasswordVerificationResult.Failed);
        result.User.Role.ShouldBe("Customer");
        result.AccessToken.ShouldBe("access-token");
    }

    // --- Login ----------------------------------------------------------

    [Fact]
    public async Task Login_with_unknown_email_throws_unauthorized()
    {
        var dto = new LoginDto { Email = "nobody@example.com", Password = Password };

        await Should.ThrowAsync<UnauthorizedException>(() => _sut.LoginAsync(dto, Ct));
    }

    [Fact]
    public async Task Login_with_wrong_password_throws_the_same_generic_error_as_unknown_email()
    {
        ExistingUser();
        var dto = new LoginDto { Email = "erick@example.com", Password = "WrongPassword1" };

        var ex = await Should.ThrowAsync<UnauthorizedException>(() => _sut.LoginAsync(dto, Ct));

        ex.Message.ShouldBe("Invalid email or password.");
    }

    [Fact]
    public async Task Login_of_a_deactivated_account_is_refused()
    {
        ExistingUser(isActive: false);
        var dto = new LoginDto { Email = "erick@example.com", Password = Password };

        await Should.ThrowAsync<UnauthorizedException>(() => _sut.LoginAsync(dto, Ct));
        await _refreshTokens.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Successful_login_persists_only_the_hash_of_the_refresh_token()
    {
        var user = ExistingUser();
        var dto = new LoginDto { Email = "erick@example.com", Password = Password };

        var result = await _sut.LoginAsync(dto, Ct);

        result.RefreshToken.ShouldBe("new-refresh-token");
        await _refreshTokens.Received(1).AddAsync(
            Arg.Is<RefreshToken>(t => t.UserId == user.Id && t.TokenHash == "hash:new-refresh-token"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // --- Refresh --------------------------------------------------------

    [Fact]
    public async Task Refresh_with_an_unknown_token_throws_unauthorized()
    {
        await Should.ThrowAsync<UnauthorizedException>(() => _sut.RefreshAsync("forged-token", Ct));
    }

    [Fact]
    public async Task Reusing_an_already_rotated_token_revokes_every_session_of_the_user()
    {
        var user = ExistingUser();
        StoredToken(user.Id, revokedAt: DateTime.UtcNow.AddMinutes(-5));

        await Should.ThrowAsync<UnauthorizedException>(() => _sut.RefreshAsync("old-refresh-token", Ct));

        await _refreshTokens.Received(1).RevokeAllActiveForUserAsync(user.Id, Arg.Any<CancellationToken>());
        await _refreshTokens.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Refresh_with_an_expired_token_throws_unauthorized()
    {
        var user = ExistingUser();
        StoredToken(user.Id, expiresAt: DateTime.UtcNow.AddMinutes(-1));

        await Should.ThrowAsync<UnauthorizedException>(() => _sut.RefreshAsync("old-refresh-token", Ct));
        await _refreshTokens.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Refresh_for_a_deactivated_user_revokes_all_sessions()
    {
        var user = ExistingUser(isActive: false);
        StoredToken(user.Id);

        await Should.ThrowAsync<UnauthorizedException>(() => _sut.RefreshAsync("old-refresh-token", Ct));

        await _refreshTokens.Received(1).RevokeAllActiveForUserAsync(user.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Valid_refresh_rotates_the_token_and_chains_it_to_its_replacement()
    {
        var user = ExistingUser();
        var stored = StoredToken(user.Id);

        var result = await _sut.RefreshAsync("old-refresh-token", Ct);

        stored.RevokedAt.ShouldNotBeNull();
        stored.ReplacedByTokenHash.ShouldBe("hash:new-refresh-token");
        _refreshTokens.Received(1).Update(stored);
        await _refreshTokens.Received(1).AddAsync(
            Arg.Is<RefreshToken>(t => t.TokenHash == "hash:new-refresh-token" && t.UserId == user.Id),
            Arg.Any<CancellationToken>());
        result.RefreshToken.ShouldBe("new-refresh-token");
        result.User.Id.ShouldBe(user.Id);
    }

    // --- Logout ---------------------------------------------------------

    [Fact]
    public async Task Logout_with_an_unknown_token_is_a_silent_no_op()
    {
        await _sut.LogoutAsync("unknown-token", Ct);

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Logout_revokes_the_presented_token()
    {
        var user = ExistingUser();
        var stored = StoredToken(user.Id);

        await _sut.LogoutAsync("old-refresh-token", Ct);

        stored.RevokedAt.ShouldNotBeNull();
        _refreshTokens.Received(1).Update(stored);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
