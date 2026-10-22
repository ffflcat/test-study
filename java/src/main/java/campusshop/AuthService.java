package campusshop;

import java.security.SecureRandom;
import java.time.Instant;
import java.util.Base64;
import java.util.List;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;
import java.util.function.Function;
import java.util.stream.Collectors;

/**
 * 登录与 Token 管理。Token 有效期 1 小时。
 */
public class AuthService {

    public static final int TOKEN_LIFETIME_SECONDS = 3600;

    private record TokenInfo(String username, Instant expiresAt) {
    }

    private final Map<String, User> users;
    private final Map<String, TokenInfo> tokens = new ConcurrentHashMap<>();
    private final SecureRandom random = new SecureRandom();

    public AuthService(List<User> users) {
        this.users = users.stream().collect(Collectors.toMap(User::username, Function.identity()));
    }

    /**
     * 登录。成功返回 Token，用户名或密码错误返回 null。
     * 用户名前后的空格会被忽略。
     */
    public String login(String username, String password) {
        String name = username.trim();
        User user = users.get(name);
        if (user == null || !user.password().equals(password))
            return null;

        byte[] bytes = new byte[24];
        random.nextBytes(bytes);
        String token = Base64.getUrlEncoder().withoutPadding().encodeToString(bytes);
        tokens.put(token, new TokenInfo(name, Instant.now().plusSeconds(TOKEN_LIFETIME_SECONDS)));
        return token;
    }

    /** 根据 Token 找到用户名；Token 无效或过期时返回 null。 */
    public String validate(String token) {
        if (token == null || token.isEmpty())
            return null;
        TokenInfo info = tokens.get(token);
        if (info != null && info.expiresAt().isAfter(Instant.now()))
            return info.username();
        return null;
    }

    public User findUser(String username) {
        return users.get(username);
    }
}
