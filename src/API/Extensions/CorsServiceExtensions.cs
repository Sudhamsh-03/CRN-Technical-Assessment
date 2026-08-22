namespace ProductsApi.Api.Extensions;

public static class CorsServiceExtensions
{
    public const string PolicyName = "DefaultCorsPolicy";

    public static IServiceCollection AddCustomCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                if (allowedOrigins.Length > 0)
                {
                    policy.WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                }
                else
                {
                    policy.AllowAnyHeader().AllowAnyMethod().SetIsOriginAllowed(_ => true);
                }
            });
        });

        return services;
    }
}
