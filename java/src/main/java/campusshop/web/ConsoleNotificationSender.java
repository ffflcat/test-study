package campusshop.web;

import campusshop.Notification;
import campusshop.NotificationSender;
import org.springframework.stereotype.Component;

/** 模拟通知：输出到控制台。 */
@Component
public class ConsoleNotificationSender implements NotificationSender {

    @Override
    public void send(Notification n) {
        System.out.println("[通知] " + n.userId() + ": " + n.message());
    }
}
