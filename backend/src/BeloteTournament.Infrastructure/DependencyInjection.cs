using BeloteTournament.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BeloteTournament.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var cs = configuration.GetConnectionString("Sqlite");
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException("ConnectionStrings:Sqlite est manquante.");

        // Enregistre le DbContext
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(cs));

        // Enregistre aussi la factory (utile pour migrations + workers)
        services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite(cs));

        return services;
    }
}
