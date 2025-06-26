using Microsoft.EntityFrameworkCore;
using ShopSaga.OrderService.Repository;
using ShopSaga.OrderService.Repository.Abstraction;
using ShopSaga.OrderService.Business.Abstraction;
using ShopSaga.OrderService.Business;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddDbContext<OrderDbContext>(options => 
    options.UseSqlServer("name=ConnectionStrings:OrderServiceDb", 
    sqlServerOptionsAction: sqlOptions => 
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    }));
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ISagaStateRepository, SagaStateRepository>();
builder.Services.AddScoped<ISagaOrchestrator, SagaOrchestrator>();
builder.Services.AddScoped<IOrderBusiness, OrderBusiness>();



builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) 
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    dbContext.Database.EnsureCreated();
}

app.Run();


