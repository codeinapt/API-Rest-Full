namespace Inventory.Api.Configuration;

public sealed record AuthenticationSettings(string Authority, string MetadataAddress, string Audience)
{
    public static AuthenticationSettings FromConfiguration(IConfiguration configuration)
    {
        return new AuthenticationSettings(
            configuration.GetRequiredValue(EnvironmentVariableNames.AuthAuthority),
            configuration.GetRequiredValue(EnvironmentVariableNames.AuthMetadataAddress),
            configuration.GetRequiredValue(EnvironmentVariableNames.AuthAudience));
    }
}
