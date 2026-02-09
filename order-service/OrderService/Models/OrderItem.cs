using System;
using System.Collections.Generic;
namespace OrderService.Models;

public class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }

    public string ProductName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal Price { get; set; }

    public Order Order { get; set; } = default!;
}
