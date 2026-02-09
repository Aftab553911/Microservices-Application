using System;

namespace PaymentService.Kafka.Events;

public class PaymentCompletedEvent
{
    public Guid OrderId { get; set; }
    public DateTime PaidAt { get; set; }
    public string Status { get; set; } = "Completed";
}
