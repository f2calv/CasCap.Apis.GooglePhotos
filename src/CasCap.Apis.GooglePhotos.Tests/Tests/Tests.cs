using CasCap.Common.Extensions;
using CasCap.Models;
using CasCap.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
namespace CasCap.Apis.GooglePhotos.Tests
{
    /// <summary>
    /// Integration tests for GooglePhotos API library, update appsettings.Test.json with appropriate login values before running.
    /// </summary>
    public class Tests : TestBase
    {
        string _testFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "testdata/");

        [Fact]
        public async Task LoginTest()
        {
            var loginResult = await _googlePhotosSvc.LoginAsync();
            Assert.True(loginResult);
        }

        string GetRandomAlbumName() => $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";

        [Theory, Trait("Type", nameof(GooglePhotosService))]
        [InlineData(GooglePhotosUploadMethod.Simple)]
        [InlineData(GooglePhotosUploadMethod.ResumableSingle)]
        [InlineData(GooglePhotosUploadMethod.ResumableMultipart)]
        public async Task UploadMediaTests(GooglePhotosUploadMethod uploadMethod)
        {
            var loginResult = await _googlePhotosSvc.LoginAsync();
            Assert.True(loginResult);

            var path = $"{_testFolder}test.mp4";
            var uploadToken = await _googlePhotosSvc.UploadMediaAsync(path, uploadMethod);
            Assert.NotNull(uploadToken);
            var newMediaItemResult = await _googlePhotosSvc.AddMediaItemAsync(uploadToken, path);
            Assert.NotNull(newMediaItemResult);
            Assert.NotNull(newMediaItemResult.mediaItem);
            Assert.NotNull(newMediaItemResult.mediaItem.id);
        }

        [Fact]
        public async Task UploadSingleTests()
        {
            var loginResult = await _googlePhotosSvc.LoginAsync();
            Assert.True(loginResult);

            //upload single media item
            var mediaItem1a = await _googlePhotosSvc.UploadSingle($"{_testFolder}test1.jpg");
            Assert.NotNull(mediaItem1a);
            Assert.NotNull(mediaItem1a.mediaItem);
            Assert.NotNull(mediaItem1a.mediaItem.id);
            Assert.True(mediaItem1a.mediaItem.id.Length > 0);

            //retrieve single media item by unique id
            var mediaItem1b = await _googlePhotosSvc.GetMediaItemByIdAsync(mediaItem1a.mediaItem.id);
            Assert.NotNull(mediaItem1b);
            Assert.NotNull(mediaItem1b.id);
            Assert.True(mediaItem1b.id.Length > 0);
            Assert.True(mediaItem1b.id == mediaItem1a.mediaItem.id);

            //get or create new album
            var albumName = GetRandomAlbumName();
            var album = await _googlePhotosSvc.GetOrCreateAlbumAsync(albumName);
            Assert.NotNull(album);
            Assert.NotNull(album.id);

            //upload single media item, assign to above album
            var mediaItem2a = await _googlePhotosSvc.UploadSingle($"{_testFolder}test2.jpg", album.id);
            Assert.NotNull(mediaItem2a);
            Assert.NotNull(mediaItem2a.mediaItem);
            Assert.NotNull(mediaItem2a.mediaItem.id);
            Assert.True(mediaItem2a.mediaItem.id.Length > 0);

            //retrieve all media items from album
            var albumMediaItems = await _googlePhotosSvc.GetMediaItemsByAlbumAsync(album.id);
            Assert.NotNull(albumMediaItems);
            Assert.True(albumMediaItems.Count == 1);
        }

        [Fact]
        public async Task UploadMultipleTests()
        {
            var loginResult = await _googlePhotosSvc.LoginAsync();
            Assert.True(loginResult);

            //upload multiple media items
            var filePaths1 = new[] { $"{_testFolder}test3.jpg", $"{_testFolder}test4.jpg" };
            var response1 = await _googlePhotosSvc.UploadMultiple(filePaths1);
            Assert.NotNull(response1);
            Assert.NotNull(response1.newMediaItemResults);
            Assert.True(response1.newMediaItemResults.Count == filePaths1.Length);

            //get or create new album
            var albumName = GetRandomAlbumName();
            var album = await _googlePhotosSvc.GetOrCreateAlbumAsync(albumName);
            Assert.NotNull(album);
            Assert.True(album.title == albumName);

            //upload multiple media items, assign to album
            var filePaths2 = new[] { $"{_testFolder}test5.jpg", $"{_testFolder}test6.jpg" };
            var response2 = await _googlePhotosSvc.UploadMultiple(filePaths2, album.id);
            Assert.NotNull(response2);
            Assert.NotNull(response2.newMediaItemResults);
            Assert.True(response2.newMediaItemResults.Count == filePaths2.Length);

            var removed = await _googlePhotosSvc.RemoveMediaItemsFromAlbumAsync(album.id, response2.newMediaItemResults.Select(p => p.mediaItem.id).ToArray());
            Assert.True(removed);
            var added = await _googlePhotosSvc.AddMediaItemsToAlbumAsync(album.id, response2.newMediaItemResults.Select(p => p.mediaItem.id).ToArray());
            Assert.True(added);

            //retrieve all albums
            var albums = await _googlePhotosSvc.GetAlbumsAsync();
            Assert.NotNull(albums);
            Assert.Contains(albums, p => p.title == albumName);
            foreach (var alb in albums.Where(p => p.title == albumName))
            {
                Debug.WriteLine($"{alb.title}\t{alb.mediaItemsCount};");
                //retrieve all media items in each album
                var albumMediaItems = await _googlePhotosSvc.GetMediaItemsByAlbumAsync(alb.id);
                Assert.NotNull(albumMediaItems);
                Assert.True(albumMediaItems.Count == alb.mediaItemsCount);
                var i = 1;
                foreach (var mediaItem in albumMediaItems)
                {
                    Debug.WriteLine($"{i}\t{mediaItem.filename}\t{mediaItem.mediaMetadata.width}x{mediaItem.mediaMetadata.height}");
                    i++;
                }
            }

            //retrieve all media items
            var mediaItems = await _googlePhotosSvc.GetMediaItemsAsync();
            Assert.NotNull(mediaItems);
            Assert.True(mediaItems.Count >= filePaths1.Length + filePaths2.Length);

            //retrieve multiple media items by unique ids
            var ids = mediaItems.Select(p => p.id).ToList();
            ids.Add("invalid-id");
            var mediaItems2 = await _googlePhotosSvc.GetMediaItemsByIdsAsync(ids.ToArray());
            Assert.NotNull(mediaItems2);
            Assert.True(ids.Count - mediaItems2.Count == 1);//should have 1 failed item
            foreach (var _mi in mediaItems2)
            {
                Debug.WriteLine(_mi.ToJSON());
            }
            Assert.True(true);
        }

        [Fact]
        public async Task FilteringTests()
        {
            var loginResult = await _googlePhotosSvc.LoginAsync();
            Assert.True(loginResult);

            contentFilter contentFilter = null;
            if (false)
#pragma warning disable CS0162 // Unreachable code detected
                contentFilter = new contentFilter
#pragma warning restore CS0162 // Unreachable code detected
                {
                    includedContentCategories = new[] { GooglePhotosContentCategoryType.PEOPLE },
                    //includedContentCategories = new[] { contentCategoryType.WEDDINGS },
                    //excludedContentCategories = new[] { contentCategoryType.PEOPLE }
                };

            dateFilter dateFilter = null;
            if (true)
                dateFilter = new dateFilter
                {
                    //dates = new date[] { new date { year = 2020 } },
                    //dates = new date[] { new date { year = 2016 } },
                    //dates = new date[] { new date { year = 2016, month = 12 } },
                    //dates = new date[] { new date { year = 2016, month = 12, day = 16 } },
                    //ranges = new range[] { new range { startDate = new startDate { year = 2016 }, endDate = new endDate { year = 2017 } } },
                    ranges = new range[] { new range { startDate = new date { year = 1900 }, endDate = new date { year = DateTime.UtcNow.Year } } },
                };
            mediaTypeFilter mediaTypeFilter = null;
            if (false)
#pragma warning disable CS0162 // Unreachable code detected
                mediaTypeFilter = new mediaTypeFilter
#pragma warning restore CS0162 // Unreachable code detected
                {
                    mediaTypes = new[] { GooglePhotosMediaType.PHOTO }
                    //mediaTypes = new[] { mediaType.VIDEO }
                };
            featureFilter featureFilter = null;
            if (false)
#pragma warning disable CS0162 // Unreachable code detected
                featureFilter = new featureFilter
#pragma warning restore CS0162 // Unreachable code detected
                {
                    includedFeatures = new[] { GooglePhotosFeatureType.FAVORITES }
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
            //Debug.WriteLine(filter.ToJSON());
            var searchResults = await _googlePhotosSvc.GetMediaItemsByFilterAsync(filter);
            Assert.NotNull(searchResults);
            foreach (var result in searchResults)
                Debug.WriteLine($"{result.filename}");
            Assert.True(true);
        }

        [Fact]
        public async Task EnrichmentsTests()
        {
            var loginResult = await _googlePhotosSvc.LoginAsync();
            Assert.True(loginResult);

            var path = $"{_testFolder}test7.jpg";
            //upload image
            var uploadToken = await _googlePhotosSvc.UploadMediaAsync(path);
            Assert.NotNull(uploadToken);
            Assert.True(uploadToken.Length > 0);

            //make a mediaItem (but no album)
            var mediaItem = await _googlePhotosSvc.AddMediaItemAsync(uploadToken, path, "my test description");

            //get or create new album
            var albumName = GetRandomAlbumName();
            var album = await _googlePhotosSvc.GetOrCreateAlbumAsync(albumName);
            Assert.NotNull(album);
            Assert.True(album.title == albumName);

            //add enrichment
            var enrichmentId1 = await _googlePhotosSvc.AddEnrichmentToAlbumAsync(album.id,
                new NewEnrichmentItem($"test enrichment {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}"),
                new AlbumPosition { position = GooglePhotosPositionType.FIRST_IN_ALBUM }
                );
            Assert.NotNull(enrichmentId1);

            //add to album
            var result1 = await _googlePhotosSvc.AddMediaItemsToAlbumAsync(album.id, new[] { mediaItem.mediaItem.id });
            Assert.True(result1);

            //add enrichment relative to media item
            var enrichmentId2 = await _googlePhotosSvc.AddEnrichmentToAlbumAsync(album.id,
                new NewEnrichmentItem("another text enrichment"),
                new AlbumPosition { position = GooglePhotosPositionType.AFTER_MEDIA_ITEM, relativeMediaItemId = mediaItem.mediaItem.id }
                );
            Assert.NotNull(enrichmentId2);
        }

        [Fact]
        public async Task SharingTests()
        {
            var loginResult = await _googlePhotosSvc.LoginAsync();
            Assert.True(loginResult);

            //get or create new album
            var albumName = GetRandomAlbumName();
            var album = await _googlePhotosSvc.GetOrCreateAlbumAsync(albumName);
            Assert.NotNull(album);
            Assert.True(album.title == albumName);

            //upload single media item
            var mediaItem = await _googlePhotosSvc.UploadSingle($"{_testFolder}test8.jpg", album.id);
            Assert.NotNull(mediaItem);
            Assert.NotNull(mediaItem.mediaItem);
            Assert.NotNull(mediaItem.mediaItem.id);
            Assert.True(mediaItem.mediaItem.id.Length > 0);

            //get album contents
            var mediaItems1 = await _googlePhotosSvc.GetMediaItemsByAlbumAsync(album.id);
            Assert.NotNull(mediaItems1);
            Assert.True(mediaItems1.Count == 1);

            //remove from album
            var result2 = await _googlePhotosSvc.RemoveMediaItemsFromAlbumAsync(album.id, new[] { mediaItem.mediaItem.id });
            Assert.True(result2);

            //get album contents
            var mediaItems2 = await _googlePhotosSvc.GetMediaItemsByAlbumAsync(album.id);
            Assert.NotNull(mediaItems2);
            Assert.True(mediaItems2.Count == 0);

            //re-add same pic to album
            var result3 = await _googlePhotosSvc.AddMediaItemsToAlbumAsync(album.id, new[] { mediaItem.mediaItem.id });
            Assert.True(result3);

            //get album contents
            var mediaItems3 = await _googlePhotosSvc.GetMediaItemsByAlbumAsync(album.id);
            Assert.NotNull(mediaItems3);
            Assert.True(mediaItems3.Count == 1);

            //enable sharing on album
            var shareInfo = await _googlePhotosSvc.ShareAlbumAsync(album.id);
            Assert.NotNull(shareInfo);
            Assert.NotNull(shareInfo.shareToken);
            Assert.True(shareInfo.shareToken.Length > 0);

            //retrieve shared albums
            var sharedAlbums = await _googlePhotosSvc.GetSharedAlbumsAsync();
            Assert.True(sharedAlbums.Count == 1);

            var sharedAlb1a = await _googlePhotosSvc.GetAlbumAsync(album.id);
            Assert.NotNull(sharedAlb1a);

            var sharedAlb1b = await _googlePhotosSvc.GetSharedAlbumAsync(shareInfo.shareToken);
            Assert.NotNull(sharedAlb1b);

            //unshare the album
            var result4 = await _googlePhotosSvc.UnShareAlbumAsync(album.id);
            Assert.True(result4);
        }
    }
}