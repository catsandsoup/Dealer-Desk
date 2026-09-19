using System;
using System.IO;
using System.Text.Json;
using QuotingEngine.Core.Models;

namespace QuotingEngine.Infrastructure.Configuration;

public static class ConfigurationManager
{
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DealerDesk",
        "dealer_config.json");

    public static DealerConfig Load()
    {
        if (File.Exists(ConfigPath))
        {
            try
            {
                var json = File.ReadAllText(ConfigPath);
                var config = JsonSerializer.Deserialize(json, DealerConfigContext.Default.DealerConfig);
                if (config != null)
                {
                    return config;
                }
            }
            catch
            {
                // If parsing fails, fall back to default
            }
        }
        
        return new DealerConfig();
    }

    public static void Save(DealerConfig config)
    {
        var dir = Path.GetDirectoryName(ConfigPath);
        if (dir != null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(config, DealerConfigContext.Default.DealerConfig);
        File.WriteAllText(ConfigPath, json);
    }
}
