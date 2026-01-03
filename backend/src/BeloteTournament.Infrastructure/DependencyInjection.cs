using BeloteTournament.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BeloteTournament.Infrastructure;

/// <summary>
/// Enregistrement des dépendances Infrastructure (DB, repos...).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var cs = configuration.GetConnectionString("Sqlite");

        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException("Connection string 'Sqlite' manquante.");

        services.AddDbContext<AppDbContext>(options => options.UseSqlite(cs));

        return services;
    }
}
