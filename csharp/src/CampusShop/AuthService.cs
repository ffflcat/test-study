using System.Security.Cryptography;

namespace CampusShop;

/// <summary>用户。</summary>
public record User(string Username, string Password, string Address, string Phone);

/// <summary>
/// 登录与 Token 管理。Token 有效期 1 小时。
/// </summary>
public class AuthService
{
    public const int TokenLifetimeSeconds = 3600;

    private readonly Dictionary<string, User> _users;
    private readonly Dictionary<string, (string Username, DateTime ExpiresAt)> _tokens = new();

    public AuthService(IEnumerable<User> users)
    {
        _users = users.ToDictionary(u => u.Username);
    }

    /// <summary>
    /// 登录。成功返回 Token，用户名或密码错误返回 null。
    /// 用户名前后的空格会被忽略。
    /// </summary>
    public string? Login(string username, string password)
    {
        var name = username.Trim();
        if (!_users.TryGetValue(name, out var user) || user.Password != password)
            return null;

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24))
            .Replace('+', '-').Replace('/', '_');
        lock (_tokens)
            _tokens[token] = (name, DateTime.UtcNow.AddSeconds(TokenLifetimeSeconds));
        return token;
    }

    /// <summary>根据 Token 找到用户名；Token 无效或过期时返回 null。</summary>
    public string? Validate(string? token)
    {
        if (string.IsNullOrEmpty(token))
            return null;
        lock (_tokens)
        {
            if (_tokens.TryGetValue(token, out var t) && t.ExpiresAt > DateTime.UtcNow)
                return t.Username;
        }
        return null;
    }

    public User? FindUser(string username) => _users.GetValueOrDefault(username);
}
