namespace Oz.Api.Services;

public static class EnvironmentValidator
{
    private static readonly (string? ConfigKey, string[] EnvVars)[] Required =
    {
        ("ConnectionStrings:Default", new[] { "ConnectionStrings__Default" }),
        ("Jwt:Secret", new[] { "Jwt__Secret", "JWT_SECRET" }),
        ("Bosta:ApiKey", new[] { "Bosta__ApiKey", "BOSTA_API_KEY" }),
        (null, new[] { "BOSTA_WEBHOOK_SECRET" }),
    };

    public static void Validate(IConfiguration config, IWebHostEnvironment env, ILogger logger)
    {
        if (env.IsDevelopment()) return;

        var missing = new List<string>();
        foreach (var (configKey, envVars) in Required)
        {
            var value = !string.IsNullOrEmpty(configKey) ? config[configKey] : null;
            if (string.IsNullOrWhiteSpace(value))
            {
                foreach (var envVar in envVars)
                {
                    value = Environment.GetEnvironmentVariable(envVar);
                    if (!string.IsNullOrWhiteSpace(value)) break;
                }
            }
            if (string.IsNullOrWhiteSpace(value)) missing.Add(string.Join(" or ", envVars));
        }

        if (missing.Count > 0)
        {
            var msg = $"Missing required environment variables: {string.Join(", ", missing)}. " +
                "Set them in your hosting control panel (e.g. MonsterASP) before starting the app.";
            logger.LogCritical(msg);
            throw new InvalidOperationException(msg);
        }
    }
}
