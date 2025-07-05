using Microsoft.EntityFrameworkCore;
using ShopSaga.OrderService.Repository;
using ShopSaga.OrderService.Repository.Abstraction;
using ShopSaga.OrderService.Business.Abstraction;
using ShopSaga.OrderService.Business;
using ShopSaga.PaymentService.ClientHttp.Abstraction;
using ShopSaga.PaymentService.ClientHttp;
using ShopSaga.StockService.ClientHttp.Abstraction;
using ShopSaga.StockService.ClientHttp;
using ShopSaga.OrderService.Business.Kafka;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddDbContext<OrderDbContext>(options => options.UseSqlServer("name=ConnectionStrings:OrderServiceDb", b => b.MigrationsAssembly("ShopSaga.OrderService.WebApi")));
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderBusiness, OrderBusiness>();

// Kafka 
builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection("Kafka"));
builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();

// HTTP Client payment
builder.Services.AddHttpClient<IPaymentHttp, PaymentHttp>(client =>
{
    var paymentServiceUrl = builder.Configuration.GetValue<string>("Services:PaymentService");
    client.BaseAddress = new Uri(paymentServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// HTTP Client stock
builder.Services.AddHttpClient<IStockHttp, StockHttp>(client =>
{
    var stockServiceUrl = builder.Configuration.GetValue<string>("Services:StockService");
    client.BaseAddress = new Uri(stockServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});



builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
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


