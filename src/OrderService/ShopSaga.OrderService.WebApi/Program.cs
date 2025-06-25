var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Da mettere dbContext, repository, services, etc.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "ShopSaga Order Service API",
        Version = "v1",
        Description = "API for managing orders and orchestrating the SAGA pattern",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "ShopSaga Team"
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction()) // Enable Swagger in production for the example
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ShopSaga Order Service API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at the app's root
    });
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.Run();


