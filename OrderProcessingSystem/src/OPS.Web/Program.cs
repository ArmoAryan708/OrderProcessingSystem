using Microsoft.EntityFrameworkCore;
using OPS.Application.Extensions;
using OPS.Application.Interfaces.Services;
using OPS.Database.DbContext;
using OPS.Database.Extensions;
using OPS.Database.Seed;
using OPS.Web.Extensions;
using OPS.Web.Middleware;
using OPS.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddApplicationServices();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddSwaggerDocumentation();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "OPS API v1"));

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DataSeeder.SeedAsync(db);
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
