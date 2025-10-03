using Microsoft.Extensions.Configuration;

namespace PipelineIntegrationTestSample.IntegrationTests;

public static class TestConfig
{
    public static IConfiguration Build()
    {
        return new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
    }
}
