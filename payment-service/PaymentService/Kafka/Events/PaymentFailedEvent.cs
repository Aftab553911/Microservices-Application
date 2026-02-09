using System;

namespace PaymentService.Kafka.Events;

public class PaymentFailedEvent
{
    public Guid OrderId { get; set; }
    public string Reason { get; set; } = "Payment declined";
    public DateTime FailedAt { get; set; }
}
