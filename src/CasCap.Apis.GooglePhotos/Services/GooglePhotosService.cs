using CasCap.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
namespace CasCap.Services
{
    /// <summary>
    /// This class chains together the inherited GooglePhotosServiceBase REST methods into more useful combos/actions.
    /// </summary>
    //https://developers.google.com/photos/library/guides/get-started
    //https://developers.google.com/photos/library/guides/authentication-authorization
    public class GooglePhotosService : GooglePhotosServiceBase
    {
        public GooglePhotosService(ILogger<GooglePhotosService> logger,
            IOptions<GooglePhotosConfig> googlePhotosConfig,
            HttpClient client
            ) : base(logger, googlePhotosConfig, client)
        {

        }

        public async Task<Album?> GetOrCreateAlbumByTitle(string albumTitle, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            var album = await GetAlbumByTitle(albumTitle, comparisonType);
            if (album is null) album = await CreateAlbum(albumTitle);
            return album;
        }

        public async Task<Album?> GetAlbumByTitle(string albumTitle, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            var albums = await GetAlbums();
            return albums.FirstOrDefault(p => p.title.Equals(albumTitle, comparisonType));
        }

        //questions: do we need to validate User as an email address?
        //questions: can you upload a graphic without a mime type and will it then return a mime type??
        //questions: when happens when you upload raw bytes (without mime type)?
        //todo: add multiple test image/video files for an integration test
        //todo: upload (nested) directory structure? w/console ui progress indicator? w/webp conversion?
        //todo: download all media items to a local cache?
        //todo: re-order all media items based on creationTime? (the default album order is when added)
        //todo: clone an album to another album
    }
}