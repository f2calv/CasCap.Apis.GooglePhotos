using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
namespace CasCap.Services
{
    public class MyBackgroundService : BackgroundService
    {
        readonly ILogger _logger;
        readonly IHostApplicationLifetime _appLifetime;
        readonly GooglePhotosService _googlePhotosSvc;

        public MyBackgroundService(ILogger<MyBackgroundService> logger, IHostApplicationLifetime appLifetime,
            GooglePhotosService googlePhotosSvc)
        {
            _logger = logger;
            _appLifetime = appLifetime;
            _googlePhotosSvc = googlePhotosSvc;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogDebug($"starting {nameof(ExecuteAsync)}...");

            if (!await _googlePhotosSvc.Login()) throw new Exception($"login failed");

            var albums = await _googlePhotosSvc.GetAlbums(50);
            foreach (var album in albums)
            {
                Console.WriteLine($"{album.id}\t{album.title}");
            }

            _appLifetime.StopApplication();
            //Debugger.Break();//todo:exit the app at this point
        }
    }
}