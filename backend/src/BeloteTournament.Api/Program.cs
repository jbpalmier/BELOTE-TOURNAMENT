using BeloteTournament.Infrastructure;
using BeloteTournament.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Railway / PaaS port binding
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// EF Core SQLite
var sqliteCs = builder.Configuration.GetConnectionString("Sqlite");
if (string.IsNullOrWhiteSpace(sqliteCs))
{
    throw new InvalidOperationException(
        "ConnectionStrings:Sqlite est manquante. (Ex: Data Source=/data/belote.db)"
    );
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(sqliteCs);
});

builder.Services.AddDbContextFactory<AppDbContext>(options =>
{
    options.UseSqlite(sqliteCs);
});

builder.Services.AddInfrastructure(builder.Configuration);

// CORS

var allowedOrigins =
    builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AppCors",
        policy =>
        {
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        }
    );
});

var app = builder.Build();

// Apply EF migrations at startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseCors("AppCors");

app.MapControllers();

app.Run();
