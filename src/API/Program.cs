using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using ProductsApi.Api.Extensions;
using ProductsApi.Api.Filters;
using ProductsApi.Api.Middleware;
using ProductsApi.Application;
using ProductsApi.Infrastructure;
using ProductsApi.Infrastructure.Data;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    builder.Services.AddScoped<ValidationFilter>();

    builder.Services.AddControllers(options =>
    {
        options.Filters.AddService<ValidationFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddCustomApiVersioning();
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddCustomCors(builder.Configuration);
    builder.Services.AddSwaggerDocumentation();
    builder.Services.AddResponseCompression();

    var app = builder.Build();

    if (!app.Environment.IsEnvironment("Testing"))
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.Migrate();
    }

    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<SecurityHeadersMiddleware>();

    app.UseResponseCompression();

    if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Products API v1");
        });
    }

    app.UseHttpsRedirection();

    app.UseCors(CorsServiceExtensions.PolicyName);

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program
{
}
