using CasCap.Common.Logging;
using CasCap.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace CasCap.Apis.GooglePhotos.Tests
{
    public abstract class TestBase
    {
        protected ILogger _logger;

        protected GooglePhotosService _googlePhotosSvc;

        public TestBase()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.Test.json", optional: false, reloadOnChange: true)
                .AddUserSecrets<TestBase>()//for local testing
                .AddEnvironmentVariables()//for CI testing
                .Build();
            
            //initiate ServiceCollection w/logging
            var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(configuration)
                .AddLogging(logging =>
                {
                    logging.AddDebug();
                    ApplicationLogging.LoggerFactory = logging.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
                });

            _logger = ApplicationLogging.LoggerFactory.CreateLogger<TestBase>();

            //add services
            services.AddGooglePhotos(options =>
            {
                options.User = "your.email@mydomain.com";
                options.Scopes = new[] { Models.GooglePhotos.Scope.Access };
                options.ClientId = "012345678901-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.apps.googleusercontent.com";
                options.ClientSecret = "abcabcabcabcabcabcabcabc";
            });

            //retrieve services
            var serviceProvider = services.BuildServiceProvider();
            _googlePhotosSvc = serviceProvider.GetRequiredService<GooglePhotosService>();
        }
    }
}