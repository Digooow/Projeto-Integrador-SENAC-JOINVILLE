using Cantina.Application.Abstractions;
using Cantina.Application.Options;
using Cantina.Application.Services;
using Cantina.Infrastructure.Persistence;
using Cantina.Infrastructure.WhatsApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cantina.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCantinaInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CantinaOperacaoOptions>(
            configuration.GetSection(CantinaOperacaoOptions.SectionName));

        services.Configure<WhatsAppOptions>(
            configuration.GetSection(WhatsAppOptions.SectionName));

        string? databaseUrlEnv = configuration["DATABASE_URL"];
        string provider = configuration["Database:Provider"] ?? "Sqlite";
        string? connStr = configuration.GetConnectionString("DefaultConnection");

        if (!string.IsNullOrWhiteSpace(databaseUrlEnv) &&
            (databaseUrlEnv.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
             databaseUrlEnv.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)))
        {
            provider = "PostgreSql";
            connStr = ConverterDatabaseUrlParaNpgsql(databaseUrlEnv);
        }

        services.AddDbContext<CantinaDbContext>(options =>
        {
            if (provider.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase) ||
                provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(connStr))
                {
                    throw new InvalidOperationException(
                        "A ConnectionString 'DefaultConnection' precisa ser informada quando o provider configurado for PostgreSQL.");
                }

                string pgConn = connStr.StartsWith("postgres", StringComparison.OrdinalIgnoreCase)
                    ? ConverterDatabaseUrlParaNpgsql(connStr)
                    : connStr;

                options.UseNpgsql(pgConn);
            }
            else
            {
                string sqliteConn = !string.IsNullOrWhiteSpace(connStr)
                    ? connStr
                    : $"Data Source={Path.Combine(AppContext.BaseDirectory, "cantina_alamo.db")}";

                options.UseSqlite(sqliteConn);
            }
        });

        services.AddScoped<ICantinaDbContext>(sp => sp.GetRequiredService<CantinaDbContext>());

        services.AddHttpClient<IWhatsAppGateway, SmartWhatsAppGateway>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddScoped<ICatalogoService, CatalogoService>();
        services.AddScoped<IPedidoService, PedidoService>();
        services.AddScoped<IGestaoCantinaService, GestaoCantinaService>();

        return services;
    }

    public static string ConverterDatabaseUrlParaNpgsql(string databaseUrl)
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':', 2);
        string user = Uri.UnescapeDataString(userInfo[0]);
        string pass = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        string db = uri.AbsolutePath.Trim('/');
        int port = uri.Port > 0 ? uri.Port : 5432;

        return $"Host={uri.Host};Port={port};Database={db};Username={user};Password={pass};SSL Mode=Prefer;Trust Server Certificate=true";
    }
}
