package campusshop;

/** 通知发送（短信、邮件等）。 */
public interface NotificationSender {
    void send(Notification notification);
}
