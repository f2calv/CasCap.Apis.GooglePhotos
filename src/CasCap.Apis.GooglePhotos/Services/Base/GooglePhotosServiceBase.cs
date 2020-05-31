using CasCap.Common.Extensions;
using CasCap.Messages;
using CasCap.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace CasCap.Services
{
    public abstract class GooglePhotosServiceBase : HttpClientBase
    {
        const int maxSizeImageBytes = 1024 * 1024 * 200;
        const long maxSizeVideoBytes = 1024 * 1024 * 1024 * 10L;

        const int minPageSizeAlbums = 1;
        const int defaultPageSizeAlbums = 50;
        const int maxPageSizeAlbums = 50;

        const int minPageSizeMediaItems = 1;
        const int defaultPageSizeMediaItems = 100;
        const int maxPageSizeMediaItems = 100;

        bool IsImage(string extension) => dMimeTypesImages.TryGetValue(extension, out var _);

        //todo: do we need to handle the mime types in a more forgiving way?
        static Dictionary<string, string> dMimeTypesImages = new Dictionary<string, string>
        {
            { ".bmp", "image/bmp" },
            { ".gif", "image/gif" },
            { ".heic", "image/heic" },
            { ".ico", "image/vnd.microsoft.icon" },
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".png", "image/png" },
            { ".tif", "image/tiff" },
            { ".tiff", "image/tiff" },
            { ".webp", "image/webp" },
        };

        bool IsVideo(string extension) => dMimeTypesVideos.TryGetValue(extension, out var _);

        //todo: do we need to handle the mime types in a more forgiving way?
        static Dictionary<string, string> dMimeTypesVideos = new Dictionary<string, string>
        {
            { ".3gp", "video/3gpp" },
            { ".3g2", "video/3gpp2" },
            { ".asf", "video/x-ms-asf" },
            { ".avi", "video/x-msvideo" },
            { ".divx", "video/divx" },
            { ".m2t", "video/mpeg" },//https://en.wikipedia.org/wiki/MPEG_transport_stream
            { ".m2ts", "video/mpeg" },//https://en.wikipedia.org/wiki/MPEG_transport_stream
            { ".m4v", "video/mp4" },
            { ".mkv", "video/x-matroska" },
            { ".mmv", "video/mmv" },//?
            { ".mod", "video/mod" },//?
            { ".mov", "video/quicktime" },
            { ".mp4", "video/mp4" },
            { ".mpg", "video/mpeg" },
            { ".mpeg", "video/mpeg" },
            { ".mts", "video/mpeg" },//https://en.wikipedia.org/wiki/MPEG_transport_stream
            { ".tod", "video/mpeg" },//https://en.wikipedia.org/wiki/MPEG_transport_stream
            { ".wmv", "video/x-ms-wmv" },
        };

        static Dictionary<GooglePhotos.Scope, string> dScopes = new Dictionary<GooglePhotos.Scope, string>
        {
            { GooglePhotos.Scope.ReadOnly , "https://www.googleapis.com/auth/photoslibrary.readonly"},
            { GooglePhotos.Scope.AppendOnly , "https://www.googleapis.com/auth/photoslibrary.appendonly"},
            { GooglePhotos.Scope.AppCreatedData , "https://www.googleapis.com/auth/photoslibrary.readonly.appcreateddata"},
            { GooglePhotos.Scope.Access , "https://www.googleapis.com/auth/photoslibrary"},
            { GooglePhotos.Scope.Sharing , "https://www.googleapis.com/auth/photoslibrary.sharing"}
        };

        GooglePhotosConfig? _googlePhotosConfig;

        public GooglePhotosServiceBase(ILogger<GooglePhotosService> logger,
            IOptions<GooglePhotosConfig> googlePhotosConfig,
            HttpClient client
            )
        {
            _logger = logger;
            _googlePhotosConfig = googlePhotosConfig.Value;// ?? throw new ArgumentNullException($"{nameof(GooglePhotosConfig)} cannot be null!");
            _client = client ?? throw new ArgumentNullException($"{nameof(HttpClient)} cannot be null!");
        }

        public async Task<bool> Login(string User, string ClientId, string ClientSecret, GooglePhotos.Scope[] Scopes, string? FileDataStoreFullPath = null)
        {
            _googlePhotosConfig = new GooglePhotosConfig
            {
                User = User,
                ClientId = ClientId,
                ClientSecret = ClientSecret,
                Scopes = Scopes,
                FileDataStoreFullPath = FileDataStoreFullPath
            };
            return await Login();
        }

        public async Task<bool> Login()
        {
            if (_googlePhotosConfig is null) throw new ArgumentNullException($"{nameof(GooglePhotosConfig)} cannot be null!");
            if (string.IsNullOrWhiteSpace(_googlePhotosConfig.User)) throw new ArgumentNullException($"{nameof(GooglePhotosConfig)}.{nameof(_googlePhotosConfig.User)} cannot be null!");
            if (string.IsNullOrWhiteSpace(_googlePhotosConfig.ClientId)) throw new ArgumentNullException($"{nameof(GooglePhotosConfig)}.{nameof(_googlePhotosConfig.ClientId)} cannot be null!");
            if (string.IsNullOrWhiteSpace(_googlePhotosConfig.ClientSecret)) throw new ArgumentNullException($"{nameof(GooglePhotosConfig)}.{nameof(_googlePhotosConfig.ClientSecret)} cannot be null!");
            if (_googlePhotosConfig.Scopes.IsNullOrEmpty()) throw new ArgumentNullException($"{nameof(GooglePhotosConfig)}.{nameof(_googlePhotosConfig.Scopes)} cannot be null/empty!");

            var secrets = new ClientSecrets { ClientId = _googlePhotosConfig.ClientId, ClientSecret = _googlePhotosConfig.ClientSecret };

            FileDataStore? dataStore = null;
            if (!string.IsNullOrWhiteSpace(_googlePhotosConfig.FileDataStoreFullPath))
                dataStore = new FileDataStore(_googlePhotosConfig.FileDataStoreFullPath, true);

            _logger.LogDebug($"Requesting authorization...");
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                secrets,
                GetScopes(),
                _googlePhotosConfig.User,
                CancellationToken.None,
                dataStore);

            _logger.LogDebug("Authorisation granted or not required (if the saved access token already available)");

            if (credential.Token.IsExpired(credential.Flow.Clock))
            {
                _logger.LogWarning("The access token has expired, refreshing it");
                if (await credential.RefreshTokenAsync(CancellationToken.None))
                    _logger.LogInformation("The access token is now refreshed");
                else
                {
                    _logger.LogError("The access token has expired but we can't refresh it :(");
                    return false;
                }
            }
            else
                _logger.LogDebug("The access token is OK, continue");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(credential.Token.TokenType, credential.Token.AccessToken);
            return true;

            string[] GetScopes()//todo: make extension method to convert any enum to string[] and move to CasCap.Common.Extensions
            {
                var l = new List<string>(_googlePhotosConfig.Scopes.Length);
                foreach (var scope in _googlePhotosConfig.Scopes)
                    if (dScopes.TryGetValue(scope, out var s))
                        l.Add(s);
                return l.ToArray();
            }
        }

        #region https://photoslibrary.googleapis.com/v1/albums

        //https://photoslibrary.googleapis.com/v1/albums/{albumId}
        public async Task<Album?> GetAlbum(string albumId)
        {
            var res = await Get<Album>(string.Format(RequestUris.GET_album, albumId));
            return res.obj;
        }

        public async Task<Album?> GetSharedAlbum(string sharedToken)
        {
            var res = await Get<Album>(string.Format(RequestUris.GET_sharedAlbum, sharedToken));
            return res.obj;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pageSize">Maximum number of albums to return in the response. Fewer albums might be returned than the specified number. The default pageSize is 20, the maximum is 50.</param>
        /// <param name="excludeNonAppCreatedData">If set, the results exclude media items that were not created by this app. Defaults to false (all albums are returned). This field is ignored if the photoslibrary.readonly.appcreateddata scope is used.</param>
        /// <returns></returns>
        public Task<List<Album>> GetAlbums(int pageSize = defaultPageSizeAlbums, bool excludeNonAppCreatedData = false)/* where T : IPagingToken where V : IEnumerable<V>, new()*/
        {
            return _GetAlbums(RequestUris.GET_albums, pageSize, excludeNonAppCreatedData);
        }

        public Task<List<Album>> GetSharedAlbums(int pageSize = defaultPageSizeAlbums, bool excludeNonAppCreatedData = false)
            => _GetAlbums(RequestUris.GET_sharedAlbums, pageSize, excludeNonAppCreatedData);

        async Task<List<Album>> _GetAlbums(string requestUri, int pageSize = defaultPageSizeAlbums, bool excludeNonAppCreatedData = false)/* where T : IPagingToken where V : IEnumerable<V>, new()*/
        {
            if (pageSize < minPageSizeAlbums || pageSize > maxPageSizeAlbums) throw new ArgumentException($"{nameof(pageSize)} must be between {minPageSizeAlbums} and {maxPageSizeAlbums}!");
            var albums = new List<Album>();
            var pageToken = string.Empty;
            while (pageToken != null)
            {
                var _requestUri = GetUrl(requestUri, pageSize, excludeNonAppCreatedData, pageToken);
                var res = await Get<albumsGetResponse>(_requestUri);
                if (res.obj is object)
                {
                    if (!res.obj.albums.IsNullOrEmpty()) albums.AddRange(res.obj.albums);
                    if (!res.obj.sharedAlbums.IsNullOrEmpty()) albums.AddRange(res.obj.sharedAlbums);
                    //if (!string.IsNullOrWhiteSpace(res.obj.nextPageToken)) //todo: raise paging event
                    pageToken = res.obj.nextPageToken;
                }
                else
                    break;
            }
            return albums;
        }

        string GetUrl(string uri, int? pageSize = defaultPageSizeAlbums, bool excludeNonAppCreatedData = false, string? pageToken = null)
        {
            var queryParams = new Dictionary<string, string>(3);
            if (pageSize.HasValue && pageSize != defaultPageSizeAlbums) queryParams.Add(nameof(pageSize), pageSize.ToString());
            if (excludeNonAppCreatedData) queryParams.Add(nameof(excludeNonAppCreatedData), excludeNonAppCreatedData.ToString());
            if (!string.IsNullOrWhiteSpace(pageToken)) queryParams.Add(nameof(pageToken), pageToken!);//todo: nullability look further into this
            var url = QueryHelpers.AddQueryString(uri, queryParams);
            _logger.LogDebug(url);
            return url;
        }

        public async Task<Album?> CreateAlbum(string title)
        {
            var req = new { album = new Album { title = title } };
            var res = await PostJson<Album>(RequestUris.POST_albums, req);
            return res.obj;
        }

        public async Task<bool> AddMediaItemsToAlbum(string albumId, string[] mediaItemIds)
        {
            var req = new { mediaItemIds };
            var res = await PostJson<string>(string.Format(RequestUris.POST_albums_batchAddMediaItems, albumId), req);
            return res.statusCode == HttpStatusCode.OK;
        }

        public async Task<bool> RemoveMediaItemsFromAlbum(string albumId, string[] mediaItemIds)
        {
            var req = new { mediaItemIds };
            var res = await PostJson<string>(string.Format(RequestUris.POST_albums_batchRemoveMediaItems, albumId), req);
            return res.statusCode == HttpStatusCode.OK;
        }

        public async Task<enrichmentItem?> AddEnrichmentToAlbum(string albumId, NewEnrichmentItem newEnrichmentItem, AlbumPosition albumPosition)
        {
            var res = await PostJson<AddEnrichmentResponse>(string.Format(RequestUris.POST_albums_addEnrichment, albumId), new AddEnrichmentRequest(newEnrichmentItem, albumPosition));
            return res.obj != null && res.obj.enrichmentItem != null ? res.obj.enrichmentItem : null;
        }

        public async Task<ShareInfo?> ShareAlbum(string albumId, bool isCollaborative = true, bool isCommentable = true)
        {
            var req = new { sharedAlbumOptions = new SharedAlbumOptions { isCollaborative = isCollaborative, isCommentable = isCommentable } };
            var res = await PostJson<sharedAlbumResponse>(string.Format(RequestUris.POST_share, albumId), req);
            return res.obj != null && res.obj.shareInfo != null ? res.obj.shareInfo : null;
        }

        public async Task<bool> UnShareAlbum(string albumId)
        {
            var res = await PostJson<string>(string.Format(RequestUris.POST_unshare, albumId), new { });
            return res.statusCode == HttpStatusCode.OK;
        }

        public async Task<Album?> JoinSharedAlbum(string shareToken)
        {
            var res = await PostJson<Album>(RequestUris.POST_sharedAlbums_join, new { shareToken });
            return res.obj;
        }

        public async Task<bool> LeaveSharedAlbum(string shareToken)
        {
            var res = await PostJson<string>(RequestUris.POST_sharedAlbums_leave, new { shareToken });
            return res.statusCode == HttpStatusCode.OK;
        }
        #endregion

        #region https://photoslibrary.googleapis.com/v1/mediaItems
        async Task<List<MediaItem>> _GetMediaItems(int? pageSize, bool excludeNonAppCreatedData, string requestUri)
        {
            if (pageSize.HasValue && (pageSize < minPageSizeMediaItems || pageSize > maxPageSizeMediaItems)) throw new ArgumentException($"{nameof(pageSize)} must be between {minPageSizeMediaItems} and {maxPageSizeMediaItems}!");

            var mediaItems = new List<MediaItem>();
            var pageToken = string.Empty;
            while (pageToken != null)
            {
                var _requestUri = GetUrl(requestUri, pageSize, excludeNonAppCreatedData, pageToken);
                var res = await Get<mediaItemsResponse>(_requestUri);
                if (res.obj is object)
                {
                    if (!res.obj.mediaItems.IsNullOrEmpty()) mediaItems.AddRange(res.obj.mediaItems);
                    //if (!string.IsNullOrWhiteSpace(res.obj.nextPageToken)) //todo: raise paging event
                    pageToken = res.obj.nextPageToken;
                }
                else
                    break;
            }
            return mediaItems;
        }

        async Task<List<MediaItem>> _GetMediaItems(string? albumId, int pageSize, Filter? filters, bool excludeNonAppCreatedData, string requestUri)
        {
            if (filters != null && excludeNonAppCreatedData) filters.excludeNonAppCreatedData = excludeNonAppCreatedData;
            var mediaItems = new List<MediaItem>();
            var pageToken = string.Empty;
            while (pageToken != null)
            {
                var req = new { albumId, pageSize, pageToken, filters };
                var res = await PostJson<mediaItemsResponse>(requestUri, req);
                if (res.obj is object)
                {
                    if (!res.obj.mediaItems.IsNullOrEmpty()) mediaItems.AddRange(res.obj.mediaItems);
                    //if (!string.IsNullOrWhiteSpace(res.nextPageToken)) //todo: raise paging event
                    pageToken = res.obj.nextPageToken;
                }
                else
                    break;
            }
            return mediaItems;
        }

        public Task<List<MediaItem>> GetMediaItems(int pageSize = defaultPageSizeMediaItems, bool excludeNonAppCreatedData = false)
        {
            return _GetMediaItems(pageSize, excludeNonAppCreatedData, RequestUris.GET_mediaItems);
        }

        //https://photoslibrary.googleapis.com/v1/mediaItems/media-item-id
        public async Task<MediaItem?> GetMediaItemById(string mediaItemId, bool excludeNonAppCreatedData = false)
        {
            var res = await Get<MediaItem>($"{RequestUris.GET_mediaItems}/{mediaItemId}");
            return res.obj;
        }

        //https://photoslibrary.googleapis.com/v1/mediaItems:batchGet?mediaItemIds=media-item-id&mediaItemIds=another-media-item-id&mediaItemIds=incorrect-media-item-id
        public async Task<mediaItemsGetResponse?> GetMediaItemsByIds(string[] mediaItemIds)
        {
            //see https://github.com/dotnet/aspnetcore/issues/7945 can't use QueryHelpers.AddQueryString here wait for .net 5
            //var queryParams = new Dictionary<string, string>(mediaItemIds.Length);
            //foreach (var mediaItemId in mediaItemIds)
            //    queryParams.Add(nameof(mediaItemIds), mediaItemId);
            //var url = QueryHelpers.AddQueryString(RequestUris.GET_mediaItems_batchGet, queryParams);
            var sb = new StringBuilder();
            foreach (var mediaItemId in mediaItemIds)
                sb.Append($"&{nameof(mediaItemIds)}={mediaItemId}");
            var url = $"{RequestUris.GET_mediaItems_batchGet}?{sb.ToString().Substring(1)}";
            var res = await Get<mediaItemsGetResponse>(url);
            return res.obj;
        }

        public Task<List<MediaItem>> GetMediaItemsByAlbum(string albumId, int pageSize = defaultPageSizeMediaItems, bool excludeNonAppCreatedData = false)
        {
            return _GetMediaItems(albumId, pageSize, null, excludeNonAppCreatedData, RequestUris.POST_mediaItems_search);
        }

        public Task<List<MediaItem>> GetMediaItemsByFilter(Filter filter)
        {
            //validate/tidy outgoing filter object
            var contentFilter = filter.contentFilter;
            if (contentFilter != null)
            {
                if (contentFilter.includedContentCategories.IsNullOrEmpty()) contentFilter.includedContentCategories = null;
                if (contentFilter.excludedContentCategories.IsNullOrEmpty()) contentFilter.excludedContentCategories = null;
                if (contentFilter.includedContentCategories is null && contentFilter.excludedContentCategories is null)
                {
                    contentFilter = null;
                    _logger.LogDebug($"{nameof(contentFilter)} element empty so removed from outgoing request");
                }
            }
            var dateFilter = filter.dateFilter;
            if (dateFilter != null)
            {
                if (dateFilter.dates.IsNullOrEmpty()) dateFilter.dates = null;
                if (dateFilter.ranges.IsNullOrEmpty()) dateFilter.ranges = null;
                if (dateFilter.dates is null && dateFilter.ranges is null)
                {
                    dateFilter = null;
                    _logger.LogDebug($"{nameof(dateFilter)} element empty so removed from outgoing request");
                }
                //do we need to validate start/end date ranges, i.e. start before end...?
            }
            var mediaTypeFilter = filter.mediaTypeFilter;
            if (mediaTypeFilter != null)
            {
                if (mediaTypeFilter.mediaTypes.IsNullOrEmpty())
                {
                    mediaTypeFilter = null;
                    _logger.LogDebug($"{nameof(mediaTypeFilter)} element empty so removed from outgoing request");
                }
            }
            var featureFilter = filter.featureFilter;
            if (featureFilter != null)
            {
                if (featureFilter.includedFeatures.IsNullOrEmpty())
                {
                    featureFilter = null;
                    _logger.LogDebug($"{nameof(featureFilter)} element empty so removed from outgoing request");
                }
            }
            return _GetMediaItems(null, 100, filter, false, RequestUris.POST_mediaItems_search);
        }

        //would need renaming if made public
        Task<NewMediaItemResult?> AddMediaItem(string uploadToken, string? fileName = null, string? description = null, string? albumId = null, AlbumPosition? albumPosition = null)
            => AddMediaItem(new UploadItem(uploadToken, fileName, description), albumId, albumPosition);

        public Task<NewMediaItemResult?> AddMediaItem(string uploadToken, string? fileName = null, string? description = null, string? albumId = null,
            GooglePhotos.PositionType positionType = GooglePhotos.PositionType.LAST_IN_ALBUM, string? relativeMediaItemId = null, string? relativeEnrichmentItemId = null)
            => AddMediaItem(new UploadItem(uploadToken, fileName, description), albumId, GetAlbumPosition(albumId, positionType, relativeMediaItemId, relativeEnrichmentItemId));

        public Task<NewMediaItemResult?> AddMediaItem(UploadItem uploadItem, string? albumId = null,
            GooglePhotos.PositionType positionType = GooglePhotos.PositionType.LAST_IN_ALBUM, string? relativeMediaItemId = null, string? relativeEnrichmentItemId = null)
            => AddMediaItem(uploadItem, albumId, GetAlbumPosition(albumId, positionType, relativeMediaItemId, relativeEnrichmentItemId));

        //would need renaming if made public
        async Task<NewMediaItemResult?> AddMediaItem(UploadItem uploadItem, string? albumId = null, AlbumPosition? albumPosition = null)
        {
            var newMediaItems = new List<UploadItem> { uploadItem };
            var res = await AddMediaItems(newMediaItems, albumId, albumPosition);
            if (res != null && !res.newMediaItemResults.IsNullOrEmpty())
                return res.newMediaItemResults[0];
            else
            {
                _logger.LogError($"Upload failure, {uploadItem.fileName}");
                return null;
            }
        }

        public Task<mediaItemsCreateResponse?> AddMediaItems(List<UploadItem> uploadItems, string? albumId = null,
            GooglePhotos.PositionType positionType = GooglePhotos.PositionType.LAST_IN_ALBUM, string? relativeMediaItemId = null, string? relativeEnrichmentItemId = null)
            => AddMediaItems(uploadItems, albumId, GetAlbumPosition(albumId, positionType, relativeMediaItemId, relativeEnrichmentItemId));

        //would need renaming if made public
        async Task<mediaItemsCreateResponse?> AddMediaItems(List<UploadItem> uploadItems, string? albumId = null, AlbumPosition? albumPosition = null)
        {
            if (uploadItems.IsNullOrEmpty())
                throw new ArgumentNullException($"Invalid {nameof(uploadItems)} quantity, must be >= 1");
            var newMediaItems = new List<NewMediaItem>(uploadItems.Count);
            foreach (var mediaItem in uploadItems)
            {
                var newMediaItem = new NewMediaItem
                {
                    description = mediaItem.description,
                    simpleMediaItem = new SimpleMediaItem
                    {
                        fileName = mediaItem.fileName,
                        uploadToken = mediaItem.uploadToken,
                    }
                };
                newMediaItems.Add(newMediaItem);
            }
            var req = new { newMediaItems, albumId, albumPosition };
            var res = await PostJson<mediaItemsCreateResponse>(RequestUris.POST_mediaItems_batchCreate, req);
            return res.obj;
        }
        #endregion

        //https://developers.google.com/photos/library/guides/upload-media
        //https://developers.google.com/photos/library/guides/upload-media#uploading-bytes
        //https://developers.google.com/photos/library/guides/resumable-uploads
        public async Task<string?> UploadMedia(string path, GooglePhotos.uploadType uploadType = GooglePhotos.uploadType.ResumableMultipart)
        {
            if (!File.Exists(path)) throw new FileNotFoundException($"can't find {path}");
            var size = new FileInfo(path).Length;

            if (size < 1) throw new Exception($"media file {path} has no data?");
            if (IsImage(Path.GetExtension(path)) && size > maxSizeImageBytes)
                throw new NotSupportedException($"Media file {path} is too big for known upload limits of {maxSizeImageBytes} bytes!");
            if (IsVideo(Path.GetExtension(path)) && size > maxSizeVideoBytes)
                throw new NotSupportedException($"Media file {path} is too big for known upload limits of {maxSizeVideoBytes} bytes!");

            var headers = new List<(string name, string value)>();
            headers.Add(("X-Goog-Upload-Content-Type", GetMimeType()));
            if (uploadType == GooglePhotos.uploadType.Simple)
                headers.Add(("X-Goog-Upload-Protocol", "raw"));
            else if (new[] { GooglePhotos.uploadType.ResumableSingle, GooglePhotos.uploadType.ResumableMultipart }.Contains(uploadType))
            {
                headers.Add(("X-Goog-Upload-Command", "start"));
                headers.Add(("X-Goog-Upload-File-Name", Path.GetFileName(path)));
                headers.Add(("X-Goog-Upload-Protocol", "resumable"));
                headers.Add(("X-Goog-Upload-Raw-Size", size.ToString()));
            }

            if (uploadType == GooglePhotos.uploadType.Simple)
            {
                var bytes = File.ReadAllBytes(path);
                var tpl = await PostBytes<string>(RequestUris.uploads, uploadType == GooglePhotos.uploadType.ResumableSingle ? Array.Empty<byte>() : bytes, headers: headers);
                return tpl.obj;
            }
            else
            {
                var tpl = await PostBytes<string>(RequestUris.uploads, Array.Empty<byte>(), headers: headers);
                tpl.responseHeaders.TryGetValues("X-Goog-Upload-URL", out var var1);
                var Upload_URL = var1.FirstOrDefault();
                Console.WriteLine($"{Upload_URL}={Upload_URL}");
                tpl.responseHeaders.TryGetValues("X-Goog-Upload-Chunk-Granularity", out var var2);
                var Upload_Chunk_Granularity = int.Parse(var2.FirstOrDefault());

                headers = new List<(string name, string value)>();

                if (uploadType == GooglePhotos.uploadType.ResumableSingle)
                {
                    headers.Add(("X-Goog-Upload-Offset", "0"));
                    headers.Add(("X-Goog-Upload-Command", "upload, finalize"));

                    //todo: for testing override bytes with a smaller value than expected
                    var bytes = File.ReadAllBytes(path);
                    tpl = await PostBytes<string>(Upload_URL, bytes, headers: headers);
                    if (tpl.httpStatusCode != HttpStatusCode.OK)
                    {
                        //we were interrupted so query the status of the last upload
                        headers = new List<(string name, string value)>();
                        headers.Add(("X-Goog-Upload-Command", "query"));

                        tpl = await PostBytes<string>(Upload_URL, bytes, headers: headers);

                        tpl.responseHeaders.TryGetValues("X-Goog-Upload-Status", out var var3);
                        //var Status = var1.FirstOrDefault();
                        tpl.responseHeaders.TryGetValues("X-Goog-Upload-Size-Received", out var var4);
                        //var bytesReceived = int.Parse(var4.FirstOrDefault());
                    }

                    return tpl.obj;
                }
                else if (uploadType == GooglePhotos.uploadType.ResumableMultipart)
                {
                    var offset = 0;
                    var attempt = 0;
                    var retryLimit = 10;//todo: move this into settings

                    using (var fs = File.OpenRead(path))
                    using (var reader = new BinaryReader(fs))
                    {
                        while (true)
                        {
                            attempt++;
                            if (attempt > retryLimit)
                                return null;

                            var lastChunk = offset + Upload_Chunk_Granularity >= size;

                            headers = new List<(string name, string value)>();
                            headers.Add(("X-Goog-Upload-Command", $"upload{(lastChunk ? ", finalize" : string.Empty)}"));
                            headers.Add(("X-Goog-Upload-Offset", offset.ToString()));

                            //todo: need to test resuming failed uploads
                            var bytes = reader.ReadBytes(Upload_Chunk_Granularity);
                            //var bytes = File.ReadAllBytes("c:/mnt/pi/test.webp");//hack/test - read from a smaller test file and see if we get failure?
                            tpl = await PostBytes<string>(Upload_URL, bytes, headers: headers);
                            if (tpl.httpStatusCode != HttpStatusCode.OK)
                            {
                                //we were interrupted so query the status of the last upload
                                headers = new List<(string name, string value)>();
                                headers.Add(("X-Goog-Upload-Command", "query"));
                                _logger.LogDebug($"");
                                tpl = await PostBytes<string>(Upload_URL, Array.Empty<byte>(), headers: headers);

                                tpl.responseHeaders.TryGetValues("X-Goog-Upload-Status", out var var3);
                                var Status = var1.FirstOrDefault();
                                //Console.WriteLine($"Status={Status}");
                                tpl.responseHeaders.TryGetValues("X-Goog-Upload-Size-Received", out var var4);
                                var bytesReceived = int.Parse(var4.FirstOrDefault());
                                //Console.WriteLine($"bytesReceived={bytesReceived}");
                                Console.WriteLine($"retryCount={attempt}\ttrying upload again...");
                            }
                            else
                            {
                                attempt = 0;//reset retry count
                                offset += Upload_Chunk_Granularity;
                                if (bytes.Length < Upload_Chunk_Granularity)
                                    break;
                            }
                        }
                        return tpl.obj;
                    }
                }
                else
                    throw new NotSupportedException($"not supported upload type '{uploadType}'");
            }

            string GetMimeType()
            {
                var fileExtension = Path.GetExtension(path);
                if (string.IsNullOrWhiteSpace(fileExtension)) throw new NotSupportedException($"Missing file extension, unable to determine mime type for; {path}");
                if (dMimeTypesImages.TryGetValue(fileExtension, out var imageMimeType))
                    return imageMimeType;
                else if (dMimeTypesVideos.TryGetValue(fileExtension, out var videoMimeType))
                    return videoMimeType;
                else
                    throw new NotSupportedException($"Cannot match file extension '{fileExtension}' from '{path}' to a known image or video mime type.");
            }
        }

        AlbumPosition? GetAlbumPosition(string? albumId, GooglePhotos.PositionType positionType, string? relativeMediaItemId, string? relativeEnrichmentItemId)
        {
            AlbumPosition? albumPosition = null;
            if (string.IsNullOrWhiteSpace(albumId)
                && (positionType != GooglePhotos.PositionType.LAST_IN_ALBUM || !string.IsNullOrWhiteSpace(relativeMediaItemId) || !string.IsNullOrWhiteSpace(relativeEnrichmentItemId)))
                throw new NotSupportedException($"cannot specify position without including an {nameof(albumId)}!");
            if (!string.IsNullOrWhiteSpace(relativeMediaItemId) && !string.IsNullOrWhiteSpace(relativeEnrichmentItemId))
                throw new NotSupportedException($"cannot specify {nameof(relativeMediaItemId)} and {nameof(relativeEnrichmentItemId)} at the same time!");
            if (positionType == GooglePhotos.PositionType.LAST_IN_ALBUM || positionType == GooglePhotos.PositionType.POSITION_TYPE_UNSPECIFIED)
            {
                //the default so ignore
            }
            else if (positionType == GooglePhotos.PositionType.FIRST_IN_ALBUM)
                albumPosition = new AlbumPosition { position = positionType };
            else if (!string.IsNullOrWhiteSpace(relativeMediaItemId))
                albumPosition = new AlbumPosition { position = positionType, relativeMediaItemId = relativeMediaItemId };
            else if (!string.IsNullOrWhiteSpace(relativeEnrichmentItemId))
                albumPosition = new AlbumPosition { position = positionType, relativeEnrichmentItemId = relativeEnrichmentItemId };
            else
                throw new NotSupportedException($"unexpected {nameof(positionType)} '{positionType}'?");
            return albumPosition;
        }
    }
}