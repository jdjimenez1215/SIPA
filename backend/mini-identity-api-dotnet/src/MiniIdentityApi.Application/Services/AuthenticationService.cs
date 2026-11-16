using System.Net.Mail;
using System.Text.RegularExpressions;
using MiniIdentityApi.Application.DTOs.Auth;
using MiniIdentityApi.Application.Interfaces;
using MiniIdentityApi.Domain.Entities;
using MiniIdentityApi.Domain.Enums;

namespace MiniIdentityApi.Application.Services;

public class AuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly TimeProvider _clock;

    public AuthenticationService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        TimeProvider? clock = null)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _clock = clock ?? TimeProvider.System;
    }

    public void Register(RegisterRequest request)
    {
        var username = (request.Username ?? string.Empty).Trim();
        var email = (request.Email ?? string.Empty).Trim();

        if (!Regex.IsMatch(username, @"^[A-Za-z0-9._-]{3,50}$"))
            throw new ArgumentException(
                "Username must be 3-50 characters and use only letters, digits, dot, dash or underscore.");

        if (!MailAddress.TryCreate(email, out var address) || address.Address != email)
            throw new ArgumentException("Email is not valid.");

        ValidatePassword(request.Password);

        if (request.EstudianteId is <= 0)
            throw new ArgumentException("EstudianteId must be a positive number when provided.");

        var existing = _userRepository.FindByUsernameOrEmail(username)
                      ?? _userRepository.FindByUsernameOrEmail(email);

        if (existing is not null)
            throw new InvalidOperationException("User already exists.");

        var salt = _passwordHasher.GenerateSalt();
        var hash = _passwordHasher.Hash(request.Password, salt);

        var credential = new Credential(hash, salt);
        var user = new User(username, email, credential, request.EstudianteId);

        _userRepository.Save(user);
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            throw new ArgumentException("Password must be at least 8 characters long.");

        if (!password.Any(char.IsUpper) ||
            !password.Any(char.IsLower) ||
            !password.Any(char.IsDigit) ||
            !password.Any(c => !char.IsLetterOrDigit(c)))
        {
            throw new ArgumentException(
                "Password must contain at least one uppercase letter, one lowercase letter, one digit and one symbol.");
        }
    }

    public AuthResponse Login(LoginRequest request)
    {
        var user = _userRepository.FindByUsernameOrEmail(request.UsernameOrEmail);

        if (user is null)
            throw new UnauthorizedAccessException("Invalid credentials.");

        if (user.Status != UserStatus.Active)
            throw new UnauthorizedAccessException("User is not active.");

        var now = _clock.GetUtcNow();
        if (user.IsLockedOut(now))
            throw new UnauthorizedAccessException("Account locked due to too many failed attempts. Try again later.");

        var isValid = _passwordHasher.Verify(
            request.Password,
            user.Credential.Salt,
            user.Credential.PasswordHash);

        if (!isValid)
        {
            user.RegisterFailedLogin(now);
            _userRepository.Save(user);
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        user.ResetFailedLogins();
        _userRepository.Save(user);

        return new AuthResponse
        {
            AccessToken = _tokenService.GenerateToken(user),
            Username = user.Username,
            Email = user.Email,
            EstudianteId = user.EstudianteId,
            Roles = user.Roles.Select(r => r.Name).ToList()
        };
    }
}