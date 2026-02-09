using System;

namespace NotificationService.Models;

public class Notification
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string Type { get; set; } = default!;
    public string Message { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}
