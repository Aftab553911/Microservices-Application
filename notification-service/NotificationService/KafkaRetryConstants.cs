namespace NotificationService.Kafka;

public static class KafkaRetryConstants
{
    public const string RetryHeader = "retry-count";
    public const int MaxRetryCount = 3;
}
