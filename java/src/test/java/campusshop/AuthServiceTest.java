package campusshop;

import org.junit.jupiter.api.Test;

import java.util.List;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertNull;

class AuthServiceTest {

    private static AuthService createService() {
        return new AuthService(List.of(new User("alice", "123456", "学生公寓 3 栋 402", "13800000001")));
    }

    @Test
    void login_validUser_returnsToken() {
        AuthService auth = createService();

        String token = auth.login("alice", "123456");

        assertNotNull(token);
        assertFalse(token.isEmpty());
        assertEquals("alice", auth.validate(token));
    }

    @Test
    void login_wrongPassword_returnsNull() {
        AuthService auth = createService();

        String token = auth.login("alice", "wrong");

        assertNull(token);
    }

    @Test
    void login_unknownUser_returnsNull() {
        AuthService auth = createService();

        String token = auth.login("nobody", "123456");

        assertNull(token);
    }
}
