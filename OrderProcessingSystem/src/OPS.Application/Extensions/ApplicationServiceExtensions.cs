using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using OPS.Application.Interfaces.Services;
using OPS.Application.Services;

namespace OPS.Application.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IAuthService, AuthService>();

        services.AddValidatorsFromAssemblyContaining<OrderService>();

        return services;
    }
}
