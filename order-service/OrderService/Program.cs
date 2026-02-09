using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Kafka.Producers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("OrderDb")));
builder.Services.AddScoped<OrderCreatedProducer>();

var app = builder.Build();

app.MapControllers();
app.MapGet("/health", () => Results.Ok("OrderService healthy"));

app.Run();
