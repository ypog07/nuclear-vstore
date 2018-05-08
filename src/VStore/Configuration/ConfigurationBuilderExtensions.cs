using Microsoft.Extensions.Configuration;

namespace NuClear.VStore.Configuration
{
    public static class ConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder UseDefaultConfiguration(this IConfigurationBuilder configurationBuilder, string basePath, string environmentName)
        {
            // delete all default configuration providers
            configurationBuilder.Sources.Clear();

            configurationBuilder.SetBasePath(basePath)
                                .AddJsonFile("appsettings.json")
                                .AddJsonFile($"appsettings.{environmentName?.ToLower()}.json")
                                .AddEnvironmentVariables("VSTORE_");

            return configurationBuilder;
        }
    }
}