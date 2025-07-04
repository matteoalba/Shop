using Microsoft.EntityFrameworkCore;
using ShopSaga.StockService.Repository;
using ShopSaga.StockService.Repository.Abstraction;
using ShopSaga.StockService.Business;
using ShopSaga.StockService.Business.Abstraction;
using ShopSaga.OrderService.ClientHttp;
using ShopSaga.OrderService.ClientHttp.Abstraction;
using ShopSaga.StockService.Business.Kafka;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddDbContext<StockDbContext>(options => options.UseSqlServer("name=ConnectionStrings:StockServiceDb", b => b.MigrationsAssembly("ShopSaga.StockService.WebApi")));
builder.Services.AddScoped<IStockRepository, StockRepository>();
builder.Services.AddScoped<IStockBusiness, StockBusiness>();

// Configure Kafka Settings
builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection("Kafka"));
builder.Services.AddHostedService<KafkaConsumerService>();

// Configurazione HTTP Client per OrderService
builder.Services.AddHttpClient<IOrderHttp, OrderHttp>(client =>
{
    // In sviluppo usa localhost, in produzione/Docker usa il nome del servizio
    var orderServiceUrl = builder.Configuration.GetValue<string>("OrderService:BaseUrl") ?? "http://localhost:5001/";
    client.BaseAddress = new Uri(orderServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<StockDbContext>();
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


