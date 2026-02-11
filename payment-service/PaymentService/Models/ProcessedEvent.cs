using System;

namespace PaymentService.Models;

public class ProcessedEvent
{
    public Guid Id { get; set; }
    public string EventKey { get; set; } = default!;
    public DateTime ProcessedAt { get; set; }
}
