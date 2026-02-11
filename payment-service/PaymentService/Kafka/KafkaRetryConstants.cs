namespace PaymentService.Kafka;

public static class KafkaRetryConstants
{
    public const int MaxRetryCount = 3;
    public const string RetryHeader = "retry-count";
}
