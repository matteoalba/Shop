var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Da mettere dbContext, repository, services, etc.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "ShopSaga Payment Service API",
        Version = "v1",
        Description = "API for processing payments in the e-commerce system",
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ShopSaga Payment Service API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at the app's root
    });
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.Run();


