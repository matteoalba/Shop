using Microsoft.EntityFrameworkCore;
using ShopSaga.PaymentService.Repository;
using ShopSaga.PaymentService.Repository.Abstraction;
using ShopSaga.PaymentService.Business;
using ShopSaga.PaymentService.Business.Abstraction;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddDbContext<PaymentDbContext>(options => options.UseSqlServer("name=ConnectionStrings:PaymentServiceDb"));
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentBusiness, PaymentBusiness>();



builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Ensure database is created and migrated
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
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


