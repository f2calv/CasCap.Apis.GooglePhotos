using CasCap.Messages;
using CasCap.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
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

        public async Task<Album?> GetOrCreateAlbumAsync(string title, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            var album = await GetAlbumByTitleAsync(title, comparisonType);
            if (album is null) album = await CreateAlbumAsync(title);
            return album;
        }

        public async Task<Album?> GetAlbumByTitleAsync(string title, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            var albums = await GetAlbumsAsync();
            return albums.FirstOrDefault(p => p.title.Equals(title, comparisonType));
        }

        public async Task<NewMediaItemResult?> UploadSingle(string path, string? albumId = null, string? description = null)
        {
            var uploadToken = await UploadMediaAsync(path);
            if (!string.IsNullOrWhiteSpace(uploadToken))
                return await AddMediaItemAsync(uploadToken!, Path.GetFileName(path), description, albumId);
            return null;
        }

        public async Task<mediaItemsCreateResponse?> UploadMultiple(string path, string? searchPattern = null, string? albumId = null)
        {
            var paths = Directory.GetFiles(path, searchPattern);
            var uploadItems = new List<UploadItem>(paths.Length);
            foreach (var filePath in paths)
            {
                var uploadToken = await UploadMediaAsync(filePath);
                if (!string.IsNullOrWhiteSpace(uploadToken))
                    uploadItems.Add(new UploadItem(uploadToken!, filePath));
                //todo: raise photo uploaded event here
            }
            return await AddMediaItemsAsync(uploadItems, albumId);
        }
    }
}