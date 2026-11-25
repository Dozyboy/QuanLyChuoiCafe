using System.Security.Cryptography;
using QuanLyChuoiCafe.Domain.Entities;
using QuanLyChuoiCafe.Domain.Exceptions;
using QuanLyChuoiCafe.Domain.Repositories;

namespace QuanLyChuoiCafe.Domain.Services;

/// <summary>Băm mật khẩu bằng PBKDF2-SHA256, mỗi mật khẩu một muối ngẫu nhiên.</summary>
public static class PasswordHasher
{
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string stored)
    {
        var parts = (stored ?? "").Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations)) return false;
        var salt = Convert.FromBase64String(parts[1]);
        var expected = Convert.FromBase64String(parts[2]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}

public class AuthService
{
    private readonly IUserRepository _users;

    public AuthService(IUserRepository users) => _users = users;

    public User Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            throw new AuthenticationException("Hãy nhập tên đăng nhập và mật khẩu.");

        var user = _users.FindByUsername(username.Trim());
        // Cùng một thông báo cho mọi trường hợp để không lộ tài khoản nào tồn tại.
        if (user == null || !user.IsActive || !PasswordHasher.Verify(password, user.PasswordHash))
            throw new AuthenticationException("Tên đăng nhập hoặc mật khẩu không đúng.");
        return user;
    }
}
