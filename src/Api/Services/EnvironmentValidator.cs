namespace Oz.Api.Services;

public static class EnvironmentValidator
{
    private static readonly string[] Required =
    {
        "ConnectionStrings__Default",
        "JWT_SECRET",
        "BOSTA_API_KEY",
        "BOSTA_WEBHOOK_SECRET",
    };

    public static void Validate(IConfiguration config, IWebHostEnvironment env)
    {
        if (env.IsDevelopment()) return;

        var missing = new List<string>();
        foreach (var key in Required)
        {
            var value = config[key] ?? Environment.GetEnvironmentVariable(key);
            if (string.IsNullOrWhiteSpace(value)) missing.Add(key);
        }

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Missing required environment variables: {string.Join(", ", missing)}. " +
                "Set them in your hosting control panel (e.g. MonsterASP) before starting the app.");
        }
    }
}
