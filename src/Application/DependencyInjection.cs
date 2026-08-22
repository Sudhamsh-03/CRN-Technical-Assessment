using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ProductsApi.Application.Interfaces;
using ProductsApi.Application.Services;

namespace ProductsApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
