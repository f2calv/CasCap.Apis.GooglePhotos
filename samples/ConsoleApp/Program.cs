using CasCap.Models;
using CasCap.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net;
namespace CasCap;

class Program
{
    static readonly string _user = null;//e.g. "your.email@mydomain.com";
    static readonly string _clientId = null;//e.g. "012345678901-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.apps.googleusercontent.com";
    static readonly string _clientSecret = null;//e.g. "abcabcabcabcabcabcabcabc";
    const string _testFolder = "c:/temp/GooglePhotos/";//local folder of test media files

    static GooglePhotosService _googlePhotosSvc;

    static async Task Main(string[] args)
    {
        if (new[] { _user, _clientId, _clientSecret }.Any(p => string.IsNullOrWhiteSpace(p)))
        {
            Console.WriteLine("Please populate authentication details to continue...");
            Debugger.Break();
            return;
        }
        if (!Directory.Exists(_testFolder))
        {
            Console.WriteLine($"Cannot find folder '{_testFolder}'");
            Debugger.Break();
            return;
        }

        //1) new-up some basic logging (if using appsettings.json you could load logging configuration from there)
        //var configuration = new ConfigurationBuilder().Build();
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            //builder.AddConfiguration(configuration.GetSection("Logging")).AddDebug().AddConsole();
        });
        var logger = loggerFactory.CreateLogger<GooglePhotosService>();

        //2) create a configuration object
        var options = new GooglePhotosOptions
        {
            User = _user,
            ClientId = _clientId,
            ClientSecret = _clientSecret,
            //FileDataStoreFullPathOverride = _testFolder,
            Scopes = new[] { GooglePhotosScope.Access, GooglePhotosScope.Sharing },//Access+Sharing == full access
        };

        //3) (Optional) display local OAuth 2.0 JSON file(s);
        var path = options.FileDataStoreFullPathOverride is null ? options.FileDataStoreFullPathDefault : options.FileDataStoreFullPathOverride;
        Console.WriteLine($"{nameof(options.FileDataStoreFullPathOverride)}:\t{path}");
        var files = Directory.GetFiles(path);
        if (files.Length == 0)
            Console.WriteLine($"\t- n/a this is probably the first time we have authenticated...");
        else
        {
            Console.WriteLine($"Files;");
            foreach (var file in files)
                Console.WriteLine($"\t- {Path.GetFileName(file)}");
        }
        //4) create a single HttpClient, this will be efficiently re-used by GooglePhotosService
        var handler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate };
        var client = new HttpClient(handler) { BaseAddress = new Uri(options.BaseAddress) };

        //5) new-up the GooglePhotosService passing in the previous references (in lieu of dependency injection)
        _googlePhotosSvc = new GooglePhotosService(logger, Options.Create(options), client);

        //6) log-in
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
        var albumMediaItems = await _googlePhotosSvc.GetMediaItemsByAlbumAsync(album.id).ToListAsync();
        if (albumMediaItems is null) throw new Exception("retrieve media items by album id failed!");
        var i = 1;
        foreach (var item in albumMediaItems)
        {
            Console.WriteLine($"{i}\t{item.filename}\t{item.mediaMetadata.width}x{item.mediaMetadata.height}");
            i++;
        }
    }
}