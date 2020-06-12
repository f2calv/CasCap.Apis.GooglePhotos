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

        public async Task<NewMediaItemResult?> UploadSingle(string path, string? albumId = null, string? description = null, GooglePhotosUploadMethod uploadMethod = GooglePhotosUploadMethod.ResumableMultipart)
        {
            var uploadToken = await UploadMediaAsync(path, uploadMethod);
            if (!string.IsNullOrWhiteSpace(uploadToken))
                return await AddMediaItemAsync(uploadToken!, path, description, albumId);
            return null;
        }

        public Task<mediaItemsCreateResponse?> UploadMultiple(string[] filePaths, string? albumId = null, GooglePhotosUploadMethod uploadMethod = GooglePhotosUploadMethod.ResumableMultipart)
            => _UploadMultiple(filePaths, albumId, uploadMethod);

        public Task<mediaItemsCreateResponse?> UploadMultiple(string folderPath, string? searchPattern = null, string? albumId = null, GooglePhotosUploadMethod uploadMethod = GooglePhotosUploadMethod.ResumableMultipart)
        {
            var filePaths = Directory.GetFiles(folderPath, searchPattern);
            return _UploadMultiple(filePaths, albumId, uploadMethod);
        }

        async Task<mediaItemsCreateResponse?> _UploadMultiple(string[] filePaths, string? albumId = null, GooglePhotosUploadMethod uploadMethod = GooglePhotosUploadMethod.ResumableMultipart)
        {
            var uploadItems = new List<UploadItem>(filePaths.Length);
            foreach (var filePath in filePaths)
            {
                var uploadToken = await UploadMediaAsync(filePath, uploadMethod);
                if (!string.IsNullOrWhiteSpace(uploadToken))
                    uploadItems.Add(new UploadItem(uploadToken!, filePath));
                //todo: raise photo uploaded event here
            }
            return await AddMediaItemsAsync(uploadItems, albumId);
        }
    }
}