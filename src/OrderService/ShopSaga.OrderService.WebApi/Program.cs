using Microsoft.EntityFrameworkCore;
using ShopSaga.OrderService.Repository;
using ShopSaga.OrderService.Repository.Abstraction;
using ShopSaga.OrderService.Business.Abstraction;
using ShopSaga.OrderService.Business;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddDbContext<OrderDbContext>(options => options.UseSqlServer("name=ConnectionStrings:OrderServiceDb"));
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ISagaStateRepository, SagaStateRepository>();
builder.Services.AddScoped<ISagaOrchestrator, SagaOrchestrator>();
builder.Services.AddScoped<IOrderBusiness, OrderBusiness>();



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


