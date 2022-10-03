using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace CasCap.Services;

public class MyBackgroundService : BackgroundService
{
    readonly ILogger _logger;
    readonly IHostApplicationLifetime _appLifetime;
    readonly GooglePhotosService _googlePhotosSvc;

    const string _testFolder = "c:/temp/GooglePhotos/";//local folder of test media files

    public MyBackgroundService(ILogger<MyBackgroundService> logger, IHostApplicationLifetime appLifetime,
        GooglePhotosService googlePhotosSvc)
    {
        _logger = logger;
        _appLifetime = appLifetime;
        _googlePhotosSvc = googlePhotosSvc;
    }

    protected async override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug($"starting {nameof(ExecuteAsync)}...");

        //log-in
        if (!await _googlePhotosSvc.LoginAsync()) throw new Exception($"login failed!");

        //get existing/create new album
        var albumTitle = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}-{Guid.NewGuid()}";//make-up a random title
        var album = await _googlePhotosSvc.GetOrCreateAlbumAsync(albumTitle);
        if (album is null) throw new Exception("album creation failed!");
        Console.WriteLine($"{nameof(album)} '{album.title}' id is '{album.id}'");

        //upload single media item and assign to album
        var mediaItem = await _googlePhotosSvc.UploadSingle($"{_testFolder}test1.jpg", album.id);
        if (mediaItem is null) throw new Exception("media item upload failed!");
        Console.WriteLine($"{nameof(mediaItem)} '{mediaItem.mediaItem.filename}' id is '{mediaItem.mediaItem.id}'");

        //retrieve all media items in the album
        var albumMediaItems = await _googlePhotosSvc.GetMediaItemsByAlbumAsync(album.id, cancellationToken: cancellationToken).ToListAsync();
        if (albumMediaItems is null) throw new Exception("retrieve media items by album id failed!");
        var i = 1;
        foreach (var item in albumMediaItems)
        {
            Console.WriteLine($"{i}\t{item.filename}\t{item.mediaMetadata.width}x{item.mediaMetadata.height}");
            i++;
        }

        _logger.LogDebug($"exiting {nameof(ExecuteAsync)}...");
        _appLifetime.StopApplication();
    }
}