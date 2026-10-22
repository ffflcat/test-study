namespace CampusShop.Tests;

public class AuthServiceTests
{
    private static AuthService CreateService() =>
        new(new[] { new User("alice", "123456", "学生公寓 3 栋 402", "13800000001") });

    [Fact]
    public void Login_ValidUser_ReturnsToken()
    {
        var auth = CreateService();

        var token = auth.Login("alice", "123456");

        Assert.False(string.IsNullOrEmpty(token));
        Assert.Equal("alice", auth.Validate(token));
    }

    [Fact]
    public void Login_WrongPassword_ReturnsNull()
    {
        var auth = CreateService();

        var token = auth.Login("alice", "wrong");

        Assert.Null(token);
    }

    [Fact]
    public void Login_UnknownUser_ReturnsNull()
    {
        var auth = CreateService();

        var token = auth.Login("nobody", "123456");

        Assert.Null(token);
    }
}
