using System;

namespace OrderService.Kafka.Events;

public class OrderCreatedEvent
{
    public Guid OrderId { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}
