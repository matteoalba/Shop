using Microsoft.EntityFrameworkCore;
using ShopSaga.PaymentService.Repository;
using ShopSaga.PaymentService.Repository.Abstraction;
using ShopSaga.PaymentService.Business;
using ShopSaga.PaymentService.Business.Abstraction;
using ShopSaga.OrderService.ClientHttp;
using ShopSaga.OrderService.ClientHttp.Abstraction;
using ShopSaga.StockService.ClientHttp;
using ShopSaga.StockService.ClientHttp.Abstraction;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddDbContext<PaymentDbContext>(options => options.UseSqlServer("name=ConnectionStrings:PaymentServiceDb", b => b.MigrationsAssembly("ShopSaga.PaymentService.WebApi")));
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentBusiness, PaymentBusiness>();

// HTTP client per OrderService
builder.Services.AddHttpClient<IOrderHttp, OrderHttp>(client =>
{
    var orderServiceUrl = "http://localhost:5001/";
    client.BaseAddress = new Uri(orderServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// HTTP client per StockService
builder.Services.AddHttpClient<IStockHttp, StockHttp>(client =>
{
    var stockServiceUrl = "http://localhost:5003/";
    client.BaseAddress = new Uri(stockServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    context.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();


