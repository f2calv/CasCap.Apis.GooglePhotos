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
            IOptions<GooglePhotosOptions> options,
            HttpClient client
            ) : base(logger, options, client)
        {

        }

        public async Task<Album?> GetOrCreateAlbumByTitleAsync(string albumTitle, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            var album = await GetAlbumByTitleAsync(albumTitle, comparisonType);
            if (album is null) album = await CreateAlbumAsync(albumTitle);
            return album;
        }

        public async Task<Album?> GetAlbumByTitleAsync(string albumTitle, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            var albums = await GetAlbumsAsync();
            return albums.FirstOrDefault(p => p.title.Equals(albumTitle, comparisonType));
        }
    }
}