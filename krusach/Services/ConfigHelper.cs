using System;
using System.IO;
using System.Text.Json;

namespace krusach.Services
{
    public static class ConfigHelper
    {
        private static JsonDocument _config;

        static ConfigHelper()
        {
            try
            {
                var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                if (File.Exists(jsonPath))
                {
                    _config = JsonDocument.Parse(File.ReadAllText(jsonPath));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load config: {ex.Message}");
            }
        }

        public static string GetConnectionString(string name)
        {
            if (_config != null && _config.RootElement.TryGetProperty("ConnectionStrings", out var connectionStrings))
            {
                if (connectionStrings.TryGetProperty(name, out var connString))
                {
                    return connString.GetString() ?? "";
                }
            }
            return @"Server=(localdb)\MSSQLLocalDB;Database=krusach;Trusted_Connection=True;TrustServerCertificate=True;"; // Fallback
        }

        public static string GetEmailSettingString(string key, string fallback = "")
        {
            if (_config != null && _config.RootElement.TryGetProperty("EmailSettings", out var settings))
            {
                if (settings.TryGetProperty(key, out var val))
                {
                    return val.GetString() ?? fallback;
                }
            }
            return fallback;
        }

        public static int GetEmailSettingInt(string key, int fallback = 0)
        {
            if (_config != null && _config.RootElement.TryGetProperty("EmailSettings", out var settings))
            {
                if (settings.TryGetProperty(key, out var val) && val.TryGetInt32(out int intVal))
                {
                    return intVal;
                }
            }
            return fallback;
        }
    }
}
