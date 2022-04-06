using CasCap.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;
namespace CasCap.Apis.GooglePhotos.Tests;

public abstract class TestBase
{
    protected ILogger _logger;

    protected GooglePhotosService _googlePhotosSvc;

    public TestBase(ITestOutputHelper output)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile($"appsettings.Test.json", optional: false, reloadOnChange: true)
            .AddUserSecrets<TestBase>()//for local testing
            .AddEnvironmentVariables()//for CI testing
            .Build();

        //initiate ServiceCollection w/logging
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddXUnitLogging(output);

        _logger = ApplicationLogging.LoggerFactory.CreateLogger<TestBase>();

        //add services
        services.AddGooglePhotos();

        //retrieve services
        var serviceProvider = services.BuildServiceProvider();
        _googlePhotosSvc = serviceProvider.GetRequiredService<GooglePhotosService>();
    }
}