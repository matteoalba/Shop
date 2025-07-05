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

// Kafka
builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection("Kafka"));
builder.Services.AddHostedService<KafkaConsumerService>();

// HTTP per order service
builder.Services.AddHttpClient<IOrderHttp, OrderHttp>(client =>
{
    var orderServiceUrl = builder.Configuration.GetValue<string>("Services:OrderService");
    client.BaseAddress = new Uri(orderServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Applicazione automatica delle migrations e seeding al primo avvio
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var context = services.GetRequiredService<StockDbContext>();
        
        // Applica automaticamente le migrations (incluso il seeding)
        await context.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Errore durante l'inizializzazione del database StockService");
        throw;
    }
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


