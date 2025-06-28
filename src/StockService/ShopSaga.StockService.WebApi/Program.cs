using Microsoft.EntityFrameworkCore;
using ShopSaga.StockService.Repository;
using ShopSaga.StockService.Repository.Abstraction;
using ShopSaga.StockService.Business;
using ShopSaga.StockService.Business.Abstraction;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddDbContext<StockDbContext>(options => options.UseSqlServer("name=ConnectionStrings:StockServiceDb"));
builder.Services.AddScoped<IStockRepository, StockRepository>();
builder.Services.AddScoped<IStockBusiness, StockBusiness>();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<StockDbContext>();
    context.Database.EnsureCreated();
}

// Assicurati che il database sia creato in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();


