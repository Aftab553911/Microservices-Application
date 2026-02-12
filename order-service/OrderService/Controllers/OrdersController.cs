using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Data;
using OrderService.Kafka.Events;
using OrderService.Kafka.Producers;
using OrderService.Models;
using System;
using System.Threading.Tasks;

namespace OrderService.Controllers;

[ApiController]
[Authorize(Roles = "Customer,Admin")]
[Route("orders")]
public class OrdersController : ControllerBase
{
    private readonly OrderDbContext _dbContext;
    private readonly OrderCreatedProducer _producer;

    public OrdersController(
        OrderDbContext dbContext,
        OrderCreatedProducer producer)
    {
        _dbContext = dbContext;
        _producer = producer;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        // 1️⃣ Create Order entity
        var order = new Order
        {
            Id = Guid.NewGuid(),
            TotalAmount = request.TotalAmount,
            Status = "Created",
            CreatedAt = DateTime.UtcNow
        };


        // 2️⃣ Save to DB
        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();

        // 3️⃣ Publish Kafka event AFTER DB commit
        var orderEvent = new OrderCreatedEvent
        {
            OrderId = order.Id,
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt
        };

        await _producer.PublishAsync(orderEvent);

        // 4️⃣ Return response
        return Ok(new
        {
            Message = "Order saved and event published",
            OrderId = order.Id
        });
    }

    [HttpGet]
    public IActionResult Health()
    {
        return Ok("Order service is running");
    }
}
