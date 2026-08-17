using System;

namespace BeautyCommerce.Infrastructure.Configurations;

public static class ConnectionStringHelper
{
    /// <summary>
    /// Normaliza un connection string de Postgres al formato clave=valor
    /// que espera Npgsql ("Host=...;Port=...;..."). Proveedores como
    /// Render o Railway entregan la cadena en formato URI
    /// ("postgres://usuario:clave@host:puerto/db"); si ya viene en
    /// formato "Host=...", se devuelve sin cambios.
    /// </summary>
    public static string? Normalize(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        if (connectionString.TrimStart().StartsWith("Host=", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "postgres" && uri.Scheme != "postgresql"))
        {
            return connectionString;
        }

        var userInfoParts = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfoParts[0]);
        var password = userInfoParts.Length > 1
            ? Uri.UnescapeDataString(userInfoParts[1])
            : string.Empty;
        var database = uri.AbsolutePath.TrimStart('/');
        var port = uri.Port > 0 ? uri.Port : 5432;

        return $"Host={uri.Host};Port={port};Database={database};Username={username};Password={password};Ssl Mode=Require;Trust Server Certificate=true";
    }
}
