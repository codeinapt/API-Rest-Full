namespace Inventory.Api.Configuration;

public static class RequiredConfigurationExtensions
{
    public static string GetRequiredValue(this IConfiguration configuration, string environmentVariableName)
    {
        var value = configuration[environmentVariableName];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"The environment variable {environmentVariableName} is not set.");
        }

        return value;
    }
}
