using CasCap.Common.Extensions;
using CasCap.Models;
using CasCap.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
namespace CasCap
{
    class Program
    {
        static string _user = null;//e.g. "your.email@mydomain.com";
        static string _clientId = null;//e.g. "012345678901-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.apps.googleusercontent.com";
        static string _clientSecret = null;//e.g. "abcabcabcabcabcabcabcabc";
        const string _testFolder = "c:/temp/GooglePhotos/";//local folder of test media files

        static GooglePhotosService _googlePhotosSvc;

        static async Task Main(string[] args)
        {
            if (new[] { _user, _clientId, _clientSecret }.Any(p => string.IsNullOrWhiteSpace(p)))
            {
                Console.WriteLine("Please populate authentication details to continue...");
                return;
            }
            if (!Directory.Exists(_testFolder))
            {
                Console.WriteLine($"Cannot find folder '{_testFolder}'");
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
                //FileDataStoreFullPath = _testFolder,
                Scopes = new[] { GooglePhotos.Scope.Access, GooglePhotos.Scope.Sharing },//Access+Sharing == full access
            };

            //3) (Optional) display local OAuth 2.0 JSON file(s);
            var path = options.FileDataStoreFullPath is null ? options.FileDataStoreFullPathDefault : options.FileDataStoreFullPath;
            Console.WriteLine($"{nameof(options.FileDataStoreFullPath)}:\t{path}");
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

            //6) perform a log-in and then run some tests
            if (!await _googlePhotosSvc.Login()) throw new Exception($"login failed");

            await TestRun();

            //todo: move all tests into the unit tests?
            //todo: leave only simple tests here OR end to end of everything - yes better!
        }

        static async Task TestRun()
        {
            //test each upload method
            foreach (var type in Utils.GetAllItems<GooglePhotos.uploadType>())
            {
                Console.WriteLine($"type={type}");
                var path = $"{_testFolder}test.mp4";
                var uploadToken = await _googlePhotosSvc.UploadMedia(path, type);
                if (string.IsNullOrWhiteSpace(uploadToken)) throw new Exception($"upload of '{path}' failed!");
                var newMediaItemResult = await _googlePhotosSvc.AddMediaItem(uploadToken, path);
                Console.WriteLine(uploadToken);
                //Debugger.Break();
            }

            if (1 == 2)
            {
                //upload single photo, assign to album
                var myalbie = await _googlePhotosSvc.GetOrCreateAlbumByTitle("flipper");
                await UploadSingle(myalbie.id);

                //upload single photo
                await UploadSingle();

                async Task UploadSingle(string albumId = null)
                {
                    var path = $"{_testFolder}test1.jpg";
                    var uploadToken = await _googlePhotosSvc.UploadMedia(path);//todo:merge this and the below into a better helper method
                    if (string.IsNullOrWhiteSpace(uploadToken)) throw new Exception($"upload of '{path}' failed!");
                    Console.WriteLine(uploadToken);
                    var newMediaItemResult = await _googlePhotosSvc.AddMediaItem(uploadToken, path, $"this came from {path}", albumId);
                    Console.WriteLine(newMediaItemResult.ToJSON());
                    Console.WriteLine();
                }
            }
            //todo: pretty print json with highlighting

            if (1 == 2)
            {
                //upload multiple photos
                await UploadMultiple();

                //upload multiple photos, assign to album
                var myalb = await _googlePhotosSvc.GetOrCreateAlbumByTitle("flopsey");
                await UploadMultiple(myalb.id);

                async Task UploadMultiple(string albumId = null)
                {
                    var paths = Directory.GetFiles(_testFolder, "*.jpg");
                    var uploadItems = new List<UploadItem>(paths.Length);
                    foreach (var path in paths)
                    {
                        var uploadToken = await _googlePhotosSvc.UploadMedia(path);
                        if (string.IsNullOrWhiteSpace(uploadToken)) throw new Exception($"upload of '{path}' failed!");
                        Console.WriteLine(uploadToken);
                        uploadItems.Add(new UploadItem(uploadToken, path, $"this came from {path}"));
                        //raise photo uploaded event here
                    }
                    var newMediaItemResults = await _googlePhotosSvc.AddMediaItems(uploadItems, albumId);
                    Console.WriteLine(newMediaItemResults.ToJSON(Formatting.Indented));
                    Console.WriteLine();
                }
            }

            //await _googlePhotosSvc.CreateAlbum<string>("wibble");
            //await _googlePhotosSvc.CreateAlbum<string>("wobble");

            //list albums and media items within those albums
            if (1 == 2)
            {
                var albums = await _googlePhotosSvc.GetAlbums(pageSize: 5);
                foreach (var album in albums)
                {
                    Console.WriteLine($"{album.title}\t{album.mediaItemsCount};");
                    var mediaItems = await _googlePhotosSvc.GetMediaItemsByAlbum(album.id, pageSize: 25);
                    var i = 1;
                    foreach (var mediaItem in mediaItems)
                    {
                        Console.WriteLine($"{i}\t{mediaItem.filename}\t{mediaItem.mediaMetadata.photo.ToJSON()};");
                        i++;
                    }
                }
                Console.WriteLine();
            }


            //list all media items
            if (1 == 2)
            {
                var mediaItems = await _googlePhotosSvc.GetMediaItems(pageSize: 25);
                foreach (var mediaItem in mediaItems)
                    Console.WriteLine($"{mediaItem.filename}\t{mediaItem.mediaMetadata.photo.ToJSON()};");

                //var mediaItems2 = await _googlePhotosSvc.GetMediaItems();

                var mi = await _googlePhotosSvc.GetMediaItemById(mediaItems[0].id);
                Console.WriteLine(mi.ToJSON());

                var ids = mediaItems.Select(p => p.id).ToList();
                ids.Add("invalid-id");
                var mis = await _googlePhotosSvc.GetMediaItemsByIds(ids.ToArray());
                foreach (var _mi in mis.mediaItemResults)
                {
                    if (_mi.mediaItem != null)
                        Console.WriteLine(_mi.mediaItem.ToJSON());
                    else
                        Console.WriteLine(_mi.status.ToJSON());
                }
                Console.WriteLine();
            }

            if (1 == 2)
            {
                contentFilter contentFilter = null;
                if (1 == 2)
                    contentFilter = new contentFilter
                    {
                        includedContentCategories = new[] { GooglePhotos.contentCategoryType.PEOPLE },
                        //includedContentCategories = new[] { GooglePhotos.contentCategoryType.WEDDINGS },
                        //excludedContentCategories = new[] { GooglePhotos.contentCategoryType.PEOPLE }
                    };

                dateFilter dateFilter = null;
                if (1 == 1)
                    dateFilter = new dateFilter
                    {
                        //dates = new date[] { new date { year = 2020 } },
                        //dates = new date[] { new date { year = 2016 } },
                        //dates = new date[] { new date { year = 2016, month = 12 } },
                        //dates = new date[] { new date { year = 2016, month = 12, day = 16 } },
                        ranges = new range[] { new range { startDate = new startDate { year = 2016 }, endDate = new endDate { year = 2017 } } },
                    };
                mediaTypeFilter mediaTypeFilter = null;
                if (1 == 2)
                    mediaTypeFilter = new mediaTypeFilter
                    {
                        mediaTypes = new[] { GooglePhotos.mediaType.PHOTO }
                        //mediaTypes = new[] { GooglePhotos.mediaType.VIDEO }
                    };
                featureFilter featureFilter = null;
                if (1 == 1)
                    featureFilter = new featureFilter
                    {
                        includedFeatures = new[] { GooglePhotos.featureType.FAVORITES }
                    };
                var filter = new Filter
                {
                    contentFilter = contentFilter,
                    dateFilter = dateFilter,
                    mediaTypeFilter = mediaTypeFilter,
                    featureFilter = featureFilter,

                    excludeNonAppCreatedData = false,
                    includeArchivedMedia = false,
                };
                Console.WriteLine(filter.ToJSON());
                var searchResults = await _googlePhotosSvc.GetMediaItemsByFilter(filter);
                foreach (var result in searchResults)
                {
                    Console.WriteLine($"{result.filename}");
                }
            }

            ///big end to end test
            if (1 == 2)
            {
                var path = $"{_testFolder}test.jpg";
                //upload image
                var uploadToken = await _googlePhotosSvc.UploadMedia(path);
                if (string.IsNullOrWhiteSpace(uploadToken)) throw new Exception($"upload of '{path}' failed!");

                //make a mediaItem (but no album)
                var mediaItem = await _googlePhotosSvc.AddMediaItem(uploadToken, path, "my test");

                //create empty album
                var newAlb = await _googlePhotosSvc.CreateAlbum("karneval");

                var enrichmentId1 = await _googlePhotosSvc.AddEnrichmentToAlbum(newAlb.id,
                    new NewEnrichmentItem("text enrichment 123"),
                    new AlbumPosition { position = GooglePhotos.PositionType.FIRST_IN_ALBUM }
                    );

                //add to album
                if (!await _googlePhotosSvc.AddMediaItemsToAlbum(newAlb.id, new[] { mediaItem.mediaItem.id }))
                    throw new Exception($"{nameof(_googlePhotosSvc.AddMediaItemsToAlbum)} failed");

                //todo: test positioning

                var enrichmentId2 = await _googlePhotosSvc.AddEnrichmentToAlbum(newAlb.id,
                    new NewEnrichmentItem("another text enrichment"),
                    new AlbumPosition { position = GooglePhotos.PositionType.AFTER_MEDIA_ITEM, relativeMediaItemId = mediaItem.mediaItem.id }
                    );
                //get album contents
                var mediaItems = await _googlePhotosSvc.GetMediaItemsByAlbum(newAlb.id);
                if (mediaItems.Count != 1)
                    throw new Exception($"{nameof(_googlePhotosSvc.GetMediaItemsByAlbum)} failed");

                //remove from album
                if (!await _googlePhotosSvc.RemoveMediaItemsFromAlbum(newAlb.id, new[] { mediaItem.mediaItem.id }))
                    throw new Exception($"{nameof(_googlePhotosSvc.RemoveMediaItemsFromAlbum)} failed");

                //get album contents
                mediaItems = await _googlePhotosSvc.GetMediaItemsByAlbum(newAlb.id);
                if (mediaItems.Count != 0)
                    throw new Exception($"{ nameof(_googlePhotosSvc.GetMediaItemsByAlbum) } failed");

                //re-add same pic to album
                await _googlePhotosSvc.AddMediaItemsToAlbum(newAlb.id, new[] { mediaItem.mediaItem.id });

                //change album to allow sharing
                var shareInfo = await _googlePhotosSvc.ShareAlbum(newAlb.id);
                if (shareInfo is null || string.IsNullOrWhiteSpace(shareInfo.shareToken))
                    throw new Exception($"{ nameof(_googlePhotosSvc.ShareAlbum) } failed");

                var sharedAlbums = await _googlePhotosSvc.GetSharedAlbums();
                if (sharedAlbums.Count != 1)
                    throw new Exception($"{ nameof(_googlePhotosSvc.GetSharedAlbums) } failed");

                var sharedAlb1a = await _googlePhotosSvc.GetAlbum(newAlb.id);
                if (sharedAlb1a is null)
                    throw new Exception($"{ nameof(_googlePhotosSvc.GetAlbum) } failed");
                var sharedAlb1b = await _googlePhotosSvc.GetSharedAlbum(shareInfo.shareToken);
                if (sharedAlb1b is null)
                    throw new Exception($"{ nameof(_googlePhotosSvc.GetSharedAlbum) } failed");

                //unshare the album
                var unshare = await _googlePhotosSvc.UnShareAlbum(newAlb.id);
            }

            //test equality
            if (1 == 2)
            {
                //Assert.NotNull(albums);
                var album = await _googlePhotosSvc.GetOrCreateAlbumByTitle("some new title");

                var album2 = await _googlePhotosSvc.GetAlbum(album.id);

                Console.WriteLine(album.Equals(album2));
                Console.WriteLine(object.ReferenceEquals(album, album2));
            }
            Console.WriteLine();
        }
    }
}