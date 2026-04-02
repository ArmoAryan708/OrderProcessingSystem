using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OPS.Application.Interfaces;
using OPS.Application.Interfaces.Repositories;
using OPS.Database.DbContext;
using OPS.Database.Repositories;

namespace OPS.Database.Extensions;

public static class PersistenceServiceExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        return services;
    }
}
