using Microsoft.EntityFrameworkCore;
using ShopSaga.OrderService.Repository;
using ShopSaga.OrderService.Repository.Abstraction;
using ShopSaga.OrderService.Business.Abstraction;
using ShopSaga.OrderService.Business;
using ShopSaga.PaymentService.ClientHttp.Abstraction;
using ShopSaga.PaymentService.ClientHttp;
using ShopSaga.StockService.ClientHttp.Abstraction;
using ShopSaga.StockService.ClientHttp;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddDbContext<OrderDbContext>(options => options.UseSqlServer("name=ConnectionStrings:OrderServiceDb", b => b.MigrationsAssembly("ShopSaga.OrderService.WebApi")));
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderBusiness, OrderBusiness>();

// HTTP Clients
builder.Services.AddHttpClient<IPaymentHttp, PaymentHttp>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("Services:PaymentService") ?? "https://localhost:5002/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IStockHttp, StockHttp>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("Services:StockService") ?? "https://localhost:5003/");
    client.Timeout = TimeSpan.FromSeconds(30);
});



builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Ensure database is created and migrated
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    context.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) 
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();


