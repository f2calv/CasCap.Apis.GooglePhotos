using CasCap.Common.Extensions;
using CasCap.Exceptions;
using CasCap.Messages;
using CasCap.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        const int defaultBatchSizeMediaItems = 50;

        GooglePhotosOptions? _options;

        public GooglePhotosServiceBase(ILogger<GooglePhotosService> logger,
            IOptions<GooglePhotosOptions> options,
            HttpClient client
            )
        {
            _logger = logger;
            _options = options.Value;// ?? throw new ArgumentNullException(nameof(options), $"{nameof(GooglePhotosOptions)} cannot be null!");
            _client = client ?? throw new ArgumentNullException(nameof(client), $"{nameof(HttpClient)} cannot be null!");
        }

        protected virtual void RaisePagingEvent(PagingEventArgs args) => PagingEvent?.Invoke(this, args);
        public event EventHandler<PagingEventArgs>? PagingEvent;

        protected virtual void RaiseUploadProgressEvent(UploadProgressArgs args) => UploadProgressEvent?.Invoke(this, args);
        public event EventHandler<UploadProgressArgs>? UploadProgressEvent;

        public static bool IsFileUploadable(string path) => IsFileUploadableByExtension(Path.GetExtension(path));

        public static bool IsFileUploadableByExtension(string extension)
        {
            if (IsImage(extension))
                return true;
            if (IsVideo(extension))
                return true;
            return false;
        }

        static bool IsImage(string extension) => AcceptedMimeTypesImage.Contains(MimeTypeMap.GetMimeType(extension));

        static readonly HashSet<string> AcceptedMimeTypesImage = new(StringComparer.OrdinalIgnoreCase)
        {
            { "image/bmp" },
            { "image/gif" },
            { "image/heic" },
            { "image/vnd.microsoft.icon" },
            { "image/jpeg" },
            { "image/jpeg" },
            { "image/png" },
            { "image/tiff" },
            { "image/webp" },
        };

        static bool IsVideo(string extension) => AcceptedMimeTypesVideo.Contains(MimeTypeMap.GetMimeType(extension));

        //todo: do we need to handle the mime types in a more forgiving way?
        static readonly HashSet<string> AcceptedMimeTypesVideo = new(StringComparer.OrdinalIgnoreCase)
        {
            { "video/3gpp" },
            { "video/3gpp2" },
            { "video/x-ms-asf" },
            { "video/x-msvideo" },
            { "video/divx" },
            { "video/mpeg" },//https://en.wikipedia.org/wiki/MPEG_transport_stream
            { "video/mp4" },
            { "video/x-matroska" },
            { "video/mmv" },//?
            { "video/mod" },//?
            { "video/quicktime" },
            { "video/mp4" },
            { "video/mpeg" },//https://en.wikipedia.org/wiki/MPEG_transport_stream
            { "video/x-ms-wmv" },
        };

        static readonly Dictionary<GooglePhotosScope, string> dScopes = new()
        {
            { GooglePhotosScope.ReadOnly, "https://www.googleapis.com/auth/photoslibrary.readonly" },
            { GooglePhotosScope.AppendOnly, "https://www.googleapis.com/auth/photoslibrary.appendonly" },
            { GooglePhotosScope.AppCreatedData, "https://www.googleapis.com/auth/photoslibrary.readonly.appcreateddata" },
            { GooglePhotosScope.Access, "https://www.googleapis.com/auth/photoslibrary" },
            { GooglePhotosScope.Sharing, "https://www.googleapis.com/auth/photoslibrary.sharing" }
        };

        public async Task<bool> LoginAsync(string User, string ClientId, string ClientSecret, GooglePhotosScope[] Scopes, string? FileDataStoreFullPathOverride = null)
        {
            _options = new GooglePhotosOptions
            {
                User = User,
                ClientId = ClientId,
                ClientSecret = ClientSecret,
                Scopes = Scopes,
                FileDataStoreFullPathOverride = FileDataStoreFullPathOverride
            };
            return await LoginAsync();
        }

        public async Task<bool> LoginAsync(GooglePhotosOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options), $"{nameof(GooglePhotosOptions)} cannot be null!");
            return await LoginAsync();
        }

        public async Task<bool> LoginAsync()
        {
            if (_options is null) throw new ArgumentNullException(nameof(_options), $"{nameof(GooglePhotosOptions)}.{nameof(_options)} cannot be null!");
            if (string.IsNullOrWhiteSpace(_options.User)) throw new ArgumentNullException(nameof(_options.User), $"{nameof(GooglePhotosOptions)}.{nameof(_options.User)} cannot be null!");
            if (string.IsNullOrWhiteSpace(_options.ClientId)) throw new ArgumentNullException(nameof(_options.ClientId), $"{nameof(GooglePhotosOptions)}.{nameof(_options.ClientId)} cannot be null!");
            if (string.IsNullOrWhiteSpace(_options.ClientSecret)) throw new ArgumentNullException(nameof(_options.ClientSecret), $"{nameof(GooglePhotosOptions)}.{nameof(_options.ClientSecret)} cannot be null!");
            if (_options.Scopes.IsNullOrEmpty()) throw new ArgumentNullException(nameof(_options.Scopes), $"{nameof(GooglePhotosOptions)}.{nameof(_options.Scopes)} cannot be null/empty!");

            var secrets = new ClientSecrets { ClientId = _options.ClientId, ClientSecret = _options.ClientSecret };

            FileDataStore? dataStore = null;
            if (!string.IsNullOrWhiteSpace(_options.FileDataStoreFullPathOverride))
                dataStore = new FileDataStore(_options.FileDataStoreFullPathOverride, true);

            _logger.LogDebug($"Requesting authorization...");
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                secrets,
                GetScopes(),
                _options.User,
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
                var l = new List<string>(_options.Scopes.Length);
                foreach (var scope in _options.Scopes)
                    if (dScopes.TryGetValue(scope, out var s))
                        l.Add(s);
                return l.ToArray();
            }
        }

        #region https://photoslibrary.googleapis.com/v1/albums

        //https://photoslibrary.googleapis.com/v1/albums/{albumId}
        public async Task<Album?> GetAlbumAsync(string albumId)
        {
            var tpl = await Get<Album, Error>(string.Format(RequestUris.GET_album, albumId));

            return tpl.result;
        }

        public async Task<Album?> GetSharedAlbumAsync(string sharedToken)
        {
            var tpl = await Get<Album, Error>(string.Format(RequestUris.GET_sharedAlbum, sharedToken));
            if (tpl.error is object) throw new GooglePhotosException(tpl.error);

            return tpl.result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pageSize">Maximum number of albums to return in the response. Fewer albums might be returned than the specified number. The default pageSize is 20, the maximum is 50.</param>
        /// <param name="excludeNonAppCreatedData">If set, the results exclude media items that were not created by this app. Defaults to false (all albums are returned). This field is ignored if the photoslibrary.readonly.appcreateddata scope is used.</param>
        /// <returns></returns>
        public Task<List<Album>> GetAlbumsAsync(int pageSize = defaultPageSizeAlbums, bool excludeNonAppCreatedData = false, CancellationToken cancellationToken = default)/* where T : IPagingToken where V : IEnumerable<V>, new()*/
        {
            return _GetAlbumsAsync(RequestUris.GET_albums, pageSize, excludeNonAppCreatedData, cancellationToken);
        }

        public Task<List<Album>> GetSharedAlbumsAsync(int pageSize = defaultPageSizeAlbums, bool excludeNonAppCreatedData = false, CancellationToken cancellationToken = default)
            => _GetAlbumsAsync(RequestUris.GET_sharedAlbums, pageSize, excludeNonAppCreatedData, cancellationToken);

        //todo: add IPagable interface and merge with similar
        async Task<List<Album>> _GetAlbumsAsync(string requestUri, int pageSize, bool excludeNonAppCreatedData, CancellationToken cancellationToken)/* where T : IPagingToken where V : IEnumerable<V>, new()*/
        {
            if (pageSize < minPageSizeAlbums || pageSize > maxPageSizeAlbums)
                throw new ArgumentOutOfRangeException($"{nameof(pageSize)} must be between {minPageSizeAlbums} and {maxPageSizeAlbums}!");

            var l = new List<Album>();
            var pageToken = string.Empty;
            var pageNumber = 1;
            while (pageToken is object && !cancellationToken.IsCancellationRequested)
            {
                var _requestUri = GetUrl(requestUri, pageSize, excludeNonAppCreatedData, pageToken);
                var tpl = await Get<albumsGetResponse, Error>(_requestUri);
                if (tpl.error is object) throw new GooglePhotosException(tpl.error);
                else if (tpl.result is object)//to hide nullability warning
                {
                    var batch = new List<Album>(pageSize);
                    if (!tpl.result.albums.IsNullOrEmpty()) batch = tpl.result.albums ?? new List<Album>();
                    if (!tpl.result.sharedAlbums.IsNullOrEmpty()) batch = tpl.result.sharedAlbums ?? new List<Album>();
                    l.AddRange(batch);
                    if (!string.IsNullOrWhiteSpace(tpl.result.nextPageToken))
                        RaisePagingEvent(new PagingEventArgs(batch.Count, pageNumber, l.Count));
                    pageToken = tpl.result.nextPageToken;
                    pageNumber++;
                }
                else
                    break;
            }
            return l;
        }

        string GetUrl(string uri, int? pageSize = defaultPageSizeAlbums, bool excludeNonAppCreatedData = false, string? pageToken = null)
        {
            var queryParams = new Dictionary<string, string>(3);
            if (pageSize.HasValue && pageSize != defaultPageSizeAlbums) queryParams.Add(nameof(pageSize), pageSize.Value.ToString());
            if (excludeNonAppCreatedData) queryParams.Add(nameof(excludeNonAppCreatedData), excludeNonAppCreatedData.ToString());
            if (!string.IsNullOrWhiteSpace(pageToken)) queryParams.Add(nameof(pageToken), pageToken!);//todo: nullability look further into this
            var url = QueryHelpers.AddQueryString(uri, queryParams);
            _logger.LogDebug(url);
            return url;
        }

        public async Task<Album?> CreateAlbumAsync(string title)
        {
            var req = new { album = new Album { title = title } };
            var tpl = await PostJson<Album, Error>(RequestUris.POST_albums, req);
            if (tpl.error is object) throw new GooglePhotosException(tpl.error);
            return tpl.result;
        }

        public Task<bool> AddMediaItemsToAlbumAsync(string albumId, string[] mediaItemIds)
            => AddMediaItemsToAlbumAsync(albumId, mediaItemIds.ToList());

        public async Task<bool> AddMediaItemsToAlbumAsync(string albumId, List<string> mediaItemIds)
        {
            var batches = mediaItemIds.Distinct().ToList().GetBatches(defaultBatchSizeMediaItems);
            foreach (var batch in batches)
            {
                var req = new { mediaItemIds = batch.Value };
                var tpl = await PostJson<string, Error>(string.Format(RequestUris.POST_albums_batchAddMediaItems, albumId), req);
                if (tpl.error is object) throw new GooglePhotosException(tpl.error);
            }
            return true;
        }

        public Task<bool> RemoveMediaItemsFromAlbumAsync(string albumId, string[] mediaItemIds)
            => RemoveMediaItemsFromAlbumAsync(albumId, mediaItemIds.ToList());

        public async Task<bool> RemoveMediaItemsFromAlbumAsync(string albumId, List<string> mediaItemIds)
        {
            var batches = mediaItemIds.GetBatches(defaultBatchSizeMediaItems);
            foreach (var batch in batches)
            {
                var req = new { mediaItemIds = batch.Value };
                var tpl = await PostJson<string, Error>(string.Format(RequestUris.POST_albums_batchRemoveMediaItems, albumId), req);
                if (tpl.error is object) throw new GooglePhotosException(tpl.error);
            }
            return true;
        }

        public async Task<enrichmentItem?> AddEnrichmentToAlbumAsync(string albumId, NewEnrichmentItem newEnrichmentItem, AlbumPosition albumPosition)
        {
            var tpl = await PostJson<AddEnrichmentResponse, Error>(string.Format(RequestUris.POST_albums_addEnrichment, albumId), new AddEnrichmentRequest(newEnrichmentItem, albumPosition));
            if (tpl.error is object) throw new GooglePhotosException(tpl.error);
            return tpl.result is object && tpl.result.enrichmentItem is object ? tpl.result.enrichmentItem : null;
        }

        public async Task<ShareInfo?> ShareAlbumAsync(string albumId, bool isCollaborative = true, bool isCommentable = true)
        {
            var req = new { sharedAlbumOptions = new SharedAlbumOptions { isCollaborative = isCollaborative, isCommentable = isCommentable } };
            var tpl = await PostJson<sharedAlbumResponse, Error>(string.Format(RequestUris.POST_share, albumId), req);
            if (tpl.error is object) throw new GooglePhotosException(tpl.error);
            return tpl.result is object && tpl.result.shareInfo is object ? tpl.result.shareInfo : null;
        }

        public async Task<bool> UnShareAlbumAsync(string albumId)
        {
            var tpl = await PostJson<string, Error>(string.Format(RequestUris.POST_unshare, albumId), new { });
            if (tpl.error is object) throw new GooglePhotosException(tpl.error);
            return true;
        }

        public async Task<Album?> JoinSharedAlbumAsync(string shareToken)
        {
            var tpl = await PostJson<Album, Error>(RequestUris.POST_sharedAlbums_join, new { shareToken });
            if (tpl.error is object) throw new GooglePhotosException(tpl.error);
            return tpl.result;
        }

        public async Task<bool> LeaveSharedAlbumAsync(string shareToken)
        {
            var tpl = await PostJson<string, Error>(RequestUris.POST_sharedAlbums_leave, new { shareToken });
            if (tpl.error is object) throw new GooglePhotosException(tpl.error);
            return true;
        }
        #endregion

        #region https://photoslibrary.googleapis.com/v1/mediaItems
        //todo: find a neater way to merge _GetMediaItemsAsync & _GetMediaItemsViaPOSTAsync - practically the same - pass an Action?
        //todo: add IPagable interface and merge with similar
        async Task<List<MediaItem>> _GetMediaItemsAsync(int pageSize, int maxPageCount, bool excludeNonAppCreatedData, string requestUri, CancellationToken cancellationToken)
        {
            if (pageSize < minPageSizeMediaItems || pageSize > maxPageSizeMediaItems)
                throw new ArgumentOutOfRangeException($"{nameof(pageSize)} must be between {minPageSizeMediaItems} and {maxPageSizeMediaItems}!");

            var l = new List<MediaItem>();
            var pageToken = string.Empty;
            var pageNumber = 1;
            while (pageToken is object && !cancellationToken.IsCancellationRequested && pageNumber <= maxPageCount)
            {
                var _requestUri = GetUrl(requestUri, pageSize, excludeNonAppCreatedData, pageToken);
                var tpl = await Get<mediaItemsResponse, Error>(_requestUri);
                if (tpl.error is object) throw new GooglePhotosException(tpl.error);
                else if (tpl.result is object)
                {
                    var batch = new List<MediaItem>(pageSize);
                    if (!tpl.result.mediaItems.IsNullOrEmpty()) batch = tpl.result.mediaItems ?? new List<MediaItem>();
                    l.AddRange(batch);
                    if (!string.IsNullOrWhiteSpace(tpl.result.nextPageToken))
                        RaisePagingEvent(new PagingEventArgs(batch.Count, pageNumber, l.Count)
                        {
                            minDate = batch.Min(p => p.mediaMetadata.creationTime),
                            maxDate = batch.Max(p => p.mediaMetadata.creationTime),
                        });
                    pageToken = tpl.result.nextPageToken;
                    pageNumber++;
                }
                else
                    break;
            }
            return l;
        }

        //todo: add IPagable interface and merge with similar
        async Task<List<MediaItem>> _GetMediaItemsViaPOSTAsync(string? albumId, int pageSize, int maxPageCount, Filter? filters, bool excludeNonAppCreatedData, string requestUri, CancellationToken cancellationToken)
        {
            if (pageSize < minPageSizeMediaItems || pageSize > maxPageSizeMediaItems)
                throw new ArgumentOutOfRangeException($"{nameof(pageSize)} must be between {minPageSizeMediaItems} and {maxPageSizeMediaItems}!");

            if (filters is object && excludeNonAppCreatedData) filters.excludeNonAppCreatedData = excludeNonAppCreatedData;

            var l = new List<MediaItem>();
            var pageToken = string.Empty;
            var pageNumber = 1;
            while (pageToken is object && !cancellationToken.IsCancellationRequested && pageNumber <= maxPageCount)
            {
                var req = new { albumId, pageSize, pageToken, filters };
                var tpl = await PostJson<mediaItemsResponse, Error>(requestUri, req);
                if (tpl.error is object) throw new GooglePhotosException(tpl.error);
                else if (tpl.result is object)
                {
                    var batch = new List<MediaItem>(pageSize);
                    if (!tpl.result.mediaItems.IsNullOrEmpty()) batch = tpl.result.mediaItems ?? new List<MediaItem>();
                    l.AddRange(batch);
                    if (!string.IsNullOrWhiteSpace(tpl.result.nextPageToken))
                        RaisePagingEvent(new PagingEventArgs(batch.Count, pageNumber, l.Count)
                        {
                            minDate = batch.Min(p => p.mediaMetadata.creationTime),
                            maxDate = batch.Max(p => p.mediaMetadata.creationTime),
                        });
                    pageToken = tpl.result.nextPageToken;
                    pageNumber++;
                }
                else
                    break;
            }
            return l;
        }

        public Task<List<MediaItem>> GetMediaItemsAsync(int pageSize = defaultPageSizeMediaItems, int maxPageCount = int.MaxValue, bool excludeNonAppCreatedData = false, CancellationToken cancellationToken = default)
            => _GetMediaItemsAsync(pageSize, maxPageCount, excludeNonAppCreatedData, RequestUris.GET_mediaItems, cancellationToken);

        public Task<List<MediaItem>> GetMediaItemsByAlbumAsync(string albumId, int pageSize = defaultPageSizeMediaItems, int maxPageCount = int.MaxValue, bool excludeNonAppCreatedData = false, CancellationToken cancellationToken = default)
            => _GetMediaItemsViaPOSTAsync(albumId, pageSize, maxPageCount, null, excludeNonAppCreatedData, RequestUris.POST_mediaItems_search, cancellationToken);

        //https://photoslibrary.googleapis.com/v1/mediaItems/media-item-id
        public async Task<MediaItem?> GetMediaItemByIdAsync(string mediaItemId, bool excludeNonAppCreatedData = false)
        {
            var tpl = await Get<MediaItem, Error>($"{RequestUris.GET_mediaItems}/{mediaItemId}");
            if (tpl.error is object) throw new GooglePhotosException(tpl.error);
            return tpl.result;
        }

        //https://photoslibrary.googleapis.com/v1/mediaItems:batchGet?mediaItemIds=media-item-id&mediaItemIds=another-media-item-id&mediaItemIds=incorrect-media-item-id
        public Task<List<MediaItem>> GetMediaItemsByIdsAsync(string[] mediaItemIds)
            => GetMediaItemsByIdsAsync(mediaItemIds.ToList());

        public async Task<List<MediaItem>> GetMediaItemsByIdsAsync(List<string> mediaItemIds)
        {
            var l = new List<MediaItem>();
            var batches = mediaItemIds.GetBatches(defaultBatchSizeMediaItems);
            foreach (var batch in batches)
            {
                //see https://github.com/dotnet/aspnetcore/issues/7945 can't use QueryHelpers.AddQueryString here wait for .net 5
                //var queryParams = new Dictionary<string, string>(batch.Value.Length);
                //foreach (var mediaItemId in batch.Value)
                //    queryParams.Add(nameof(mediaItemIds), mediaItemId);
                //var url = QueryHelpers.AddQueryString(RequestUris.GET_mediaItems_batchGet, queryParams);
                var sb = new StringBuilder();
                foreach (var mediaItemId in batch.Value)
                    sb.Append($"&{nameof(mediaItemIds)}={mediaItemId}");
                var url = $"{RequestUris.GET_mediaItems_batchGet}?{sb.ToString().Substring(1)}";
                var tpl = await Get<mediaItemsGetResponse, Error>(url);
                if (tpl.error is object) throw new GooglePhotosException(tpl.error);
                else if (tpl.result is object)
                {
                    //l.AddRange(res.obj.mediaItemResults);
                    foreach (var result in tpl.result.mediaItemResults)
                    {
                        if (result.status is null)
                            l.Add(result.mediaItem);
                        else
                            _logger.LogWarning($"{result.status}");//we highlight if any objects returned a non-null status object
                    }
                    if (batch.Key + 1 != batches.Count)
                        RaisePagingEvent(new PagingEventArgs(tpl.result.mediaItemResults.Count, batch.Key + 1, l.Count));
                }
            }
            return l;
        }

        public Task<List<MediaItem>> GetMediaItemsByDateRangeAsync(DateTime startDate, DateTime endDate, int maxPageCount = int.MaxValue, CancellationToken cancellationToken = default)
            => GetMediaItemsByFilterAsync(new Filter(startDate, endDate), maxPageCount, cancellationToken);

        public Task<List<MediaItem>> GetMediaItemsByCategoryAsync(GooglePhotosContentCategoryType category, int maxPageCount = int.MaxValue, CancellationToken cancellationToken = default)
            => GetMediaItemsByFilterAsync(new Filter(category), maxPageCount, cancellationToken);

        public Task<List<MediaItem>> GetMediaItemsByCategoriesAsync(GooglePhotosContentCategoryType[] categories, int maxPageCount = int.MaxValue, CancellationToken cancellationToken = default)
            => GetMediaItemsByFilterAsync(new Filter(categories), maxPageCount, cancellationToken);

        public Task<List<MediaItem>> GetMediaItemsByCategoriesAsync(List<GooglePhotosContentCategoryType> categories, int maxPageCount = int.MaxValue, CancellationToken cancellationToken = default)
            => GetMediaItemsByFilterAsync(new Filter(categories), maxPageCount, cancellationToken);

        public Task<List<MediaItem>> GetMediaItemsByFilterAsync(Filter filter, int maxPageCount = int.MaxValue, CancellationToken cancellationToken = default)
            => _GetMediaItemsByFilterAsync(filter, maxPageCount, cancellationToken);

        Task<List<MediaItem>> _GetMediaItemsByFilterAsync(Filter filter, int maxPageCount, CancellationToken cancellationToken)
        {
            //validate/tidy outgoing filter object
            var contentFilter = filter.contentFilter;
            if (contentFilter is object)
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
            if (dateFilter is object)
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
            if (mediaTypeFilter is object)
            {
                if (mediaTypeFilter.mediaTypes.IsNullOrEmpty())
                {
                    mediaTypeFilter = null;
                    _logger.LogDebug($"{nameof(mediaTypeFilter)} element empty so removed from outgoing request");
                }
            }
            var featureFilter = filter.featureFilter;
            if (featureFilter is object)
            {
                if (featureFilter.includedFeatures.IsNullOrEmpty())
                {
                    featureFilter = null;
                    _logger.LogDebug($"{nameof(featureFilter)} element empty so removed from outgoing request");
                }
            }
            return _GetMediaItemsViaPOSTAsync(null, defaultPageSizeMediaItems, maxPageCount, filter, false, RequestUris.POST_mediaItems_search, cancellationToken);
        }

        //would need renaming if made public
        Task<NewMediaItemResult?> AddMediaItemAsync(string uploadToken, string? fileName = null, string? description = null, string? albumId = null, AlbumPosition? albumPosition = null)
            => AddMediaItemAsync(new UploadItem(uploadToken, fileName, description), albumId, albumPosition);

        public Task<NewMediaItemResult?> AddMediaItemAsync(string uploadToken, string? fileName = null, string? description = null, string? albumId = null,
            GooglePhotosPositionType positionType = GooglePhotosPositionType.LAST_IN_ALBUM, string? relativeMediaItemId = null, string? relativeEnrichmentItemId = null)
            => AddMediaItemAsync(new UploadItem(uploadToken, fileName, description), albumId, GetAlbumPosition(albumId, positionType, relativeMediaItemId, relativeEnrichmentItemId));

        public Task<NewMediaItemResult?> AddMediaItemAsync(UploadItem uploadItem, string? albumId = null,
            GooglePhotosPositionType positionType = GooglePhotosPositionType.LAST_IN_ALBUM, string? relativeMediaItemId = null, string? relativeEnrichmentItemId = null)
            => AddMediaItemAsync(uploadItem, albumId, GetAlbumPosition(albumId, positionType, relativeMediaItemId, relativeEnrichmentItemId));

        //would need renaming if made public
        async Task<NewMediaItemResult?> AddMediaItemAsync(UploadItem uploadItem, string? albumId, AlbumPosition? albumPosition)
        {
            var newMediaItems = new List<UploadItem> { uploadItem };
            var res = await AddMediaItemsAsync(newMediaItems, albumId, albumPosition);
            if (res is object && !res.newMediaItemResults.IsNullOrEmpty())
                return res.newMediaItemResults[0];
            else
            {
                _logger.LogError($"Upload failure, {uploadItem.fileName}");
                return null;
            }
        }

        public Task<mediaItemsCreateResponse?> AddMediaItemsAsync(List<(string uploadToken, string FileName)> items, string? albumId = null,
            GooglePhotosPositionType positionType = GooglePhotosPositionType.LAST_IN_ALBUM, string? relativeMediaItemId = null, string? relativeEnrichmentItemId = null)
        {
            var uploadItems = new List<UploadItem>(items.Count);
            foreach (var item in items)
                uploadItems.Add(new UploadItem(item.uploadToken, item.FileName));
            return AddMediaItemsAsync(uploadItems, albumId, GetAlbumPosition(albumId, positionType, relativeMediaItemId, relativeEnrichmentItemId));
        }

        public Task<mediaItemsCreateResponse?> AddMediaItemsAsync(List<UploadItem> uploadItems, string? albumId = null,
            GooglePhotosPositionType positionType = GooglePhotosPositionType.LAST_IN_ALBUM, string? relativeMediaItemId = null, string? relativeEnrichmentItemId = null)
            => AddMediaItemsAsync(uploadItems, albumId, GetAlbumPosition(albumId, positionType, relativeMediaItemId, relativeEnrichmentItemId));

        //would need renaming if made public
        async Task<mediaItemsCreateResponse?> AddMediaItemsAsync(List<UploadItem> uploadItems, string? albumId, AlbumPosition? albumPosition)
        {
            if (uploadItems.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(uploadItems), $"Invalid {nameof(uploadItems)} quantity, must be >= 1");
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
            var tpl = await PostJson<mediaItemsCreateResponse, Error>(RequestUris.POST_mediaItems_batchCreate, req);
            if (tpl.error is object) throw new GooglePhotosException(tpl.error);
            return tpl.result;
        }
        #endregion

        const string X_Goog_Upload_Content_Type = "X-Goog-Upload-Content-Type";
        const string X_Goog_Upload_Protocol = "X-Goog-Upload-Protocol";
        const string X_Goog_Upload_Command = "X-Goog-Upload-Command";
        const string X_Goog_Upload_File_Name = "X-Goog-Upload-File-Name";
        const string X_Goog_Upload_Raw_Size = "X-Goog-Upload-Raw-Size";
        const string X_Goog_Upload_URL = "X-Goog-Upload-URL";
        const string X_Goog_Upload_Offset = "X-Goog-Upload-Offset";
        const string X_Goog_Upload_Status = "X-Goog-Upload-Status";
        const string X_Goog_Upload_Chunk_Granularity = "X-Goog-Upload-Chunk-Granularity";
        const string X_Goog_Upload_Size_Received = "X-Goog-Upload-Size-Received";

        //todo: refactor this method when time, it's a bit of a mess :/
        //https://developers.google.com/photos/library/guides/upload-media
        //https://developers.google.com/photos/library/guides/upload-media#uploading-bytes
        //https://developers.google.com/photos/library/guides/resumable-uploads
        public async Task<string?> UploadMediaAsync(string path, GooglePhotosUploadMethod uploadMethod = GooglePhotosUploadMethod.ResumableMultipart, Action<int>? callback = null)
        {
            if (!File.Exists(path)) throw new FileNotFoundException($"can't find '{path}'");
            var size = new FileInfo(path).Length;

            if (size < 1) throw new Exception($"media file {path} has no data?");
            if (IsImage(Path.GetExtension(path)) && size > maxSizeImageBytes)
                throw new NotSupportedException($"Media file {path} is too big for known upload limits of {maxSizeImageBytes} bytes!");
            if (IsVideo(Path.GetExtension(path)) && size > maxSizeVideoBytes)
                throw new NotSupportedException($"Media file {path} is too big for known upload limits of {maxSizeVideoBytes} bytes!");

            var headers = new List<(string name, string value)>
            {
                (X_Goog_Upload_Content_Type, GetMimeType())
            };
            if (uploadMethod == GooglePhotosUploadMethod.Simple)
                headers.Add((X_Goog_Upload_Protocol, "raw"));
            else if (new[] { GooglePhotosUploadMethod.ResumableSingle, GooglePhotosUploadMethod.ResumableMultipart }.Contains(uploadMethod))
            {
                headers.Add((X_Goog_Upload_Command, "start"));
                headers.Add((X_Goog_Upload_File_Name, Path.GetFileName(path)));
                headers.Add((X_Goog_Upload_Protocol, "resumable"));
                headers.Add((X_Goog_Upload_Raw_Size, size.ToString()));
            }

            if (uploadMethod == GooglePhotosUploadMethod.Simple)
            {
                var bytes = File.ReadAllBytes(path);
                var tpl = await PostBytes<string, Error>(RequestUris.uploads, uploadMethod == GooglePhotosUploadMethod.ResumableSingle ? Array.Empty<byte>() : bytes, headers: headers);
                if (tpl.error is object) throw new GooglePhotosException(tpl.error);
                return tpl.result;
            }
            else
            {
                var tpl = await PostBytes<string, Error>(RequestUris.uploads, Array.Empty<byte>(), headers: headers);
                var status = tpl.responseHeaders.TryGetValue(X_Goog_Upload_Status);

                var Upload_URL = tpl.responseHeaders.TryGetValue(X_Goog_Upload_URL);
                if (Upload_URL is null) throw new Exception($"");
                //Debug.WriteLine($"{Upload_URL}={Upload_URL}");
                var sUpload_Chunk_Granularity = tpl.responseHeaders.TryGetValue(X_Goog_Upload_Chunk_Granularity);
                if (int.TryParse(sUpload_Chunk_Granularity, out var Upload_Chunk_Granularity) && Upload_Chunk_Granularity <= 0)
                    throw new Exception($"invalid {X_Goog_Upload_Chunk_Granularity}!");

                headers = new List<(string name, string value)>();

                if (uploadMethod == GooglePhotosUploadMethod.ResumableSingle)
                {
                    headers.Add((X_Goog_Upload_Offset, "0"));
                    headers.Add((X_Goog_Upload_Command, "upload, finalize"));

                    //todo: for testing override bytes with a smaller value than expected
                    var bytes = File.ReadAllBytes(path);
                    tpl = await PostBytes<string, Error>(Upload_URL, bytes, headers: headers);
                    if (tpl.httpStatusCode != HttpStatusCode.OK)
                    {
                        //we were interrupted so query the status of the last upload
                        headers = new List<(string name, string value)>
                        {
                            (X_Goog_Upload_Command, "query")
                        };

                        tpl = await PostBytes<string, Error>(Upload_URL, bytes, headers: headers);
                        if (tpl.error is object) throw new GooglePhotosException(tpl.error);

                        _ = tpl.responseHeaders.TryGetValue(X_Goog_Upload_Status);
                        _ = tpl.responseHeaders.TryGetValue(X_Goog_Upload_Size_Received);
                    }

                    return tpl.result;
                }
                else if (uploadMethod == GooglePhotosUploadMethod.ResumableMultipart)
                {
                    var offset = 0;
                    var attemptCount = 0;
                    var retryLimit = 10;//todo: move this into settings
                    var batchCount = Math.Ceiling(size / (double)Upload_Chunk_Granularity);
                    var batchIndex = 0;
                    using var fs = File.OpenRead(path);
                    using var reader = new BinaryReader(fs);
                    while (true)
                    {
                        attemptCount++;
                        if (attemptCount > retryLimit)
                            return null;

                        //var lastChunk = offset + Upload_Chunk_Granularity >= size;
                        var lastChunk = batchIndex + 1 == batchCount;

                        headers = new List<(string name, string value)>
                        {
                            (X_Goog_Upload_Command, $"upload{(lastChunk ? ", finalize" : string.Empty)}"),
                            (X_Goog_Upload_Offset, offset.ToString())
                        };

                        //todo: need to test resuming failed uploads
                        var bytes = reader.ReadBytes(Upload_Chunk_Granularity);
                        //var bytes = File.ReadAllBytes("c:/mnt/pi/test.webp");//hack/test - read from a smaller test file and see if we get failure?
                        tpl = await PostBytes<string, Error>(Upload_URL, bytes, headers: headers);
                        //if (tpl.error is object) throw new GooglePhotosAPIException(tpl.error);
                        if (tpl.httpStatusCode != HttpStatusCode.OK)
                        {
                            //we were interrupted so query the status of the last upload
                            headers = new List<(string name, string value)>
                            {
                                (X_Goog_Upload_Command, "query")
                            };
                            _logger.LogDebug($"");
                            tpl = await PostBytes<string, Error>(Upload_URL, Array.Empty<byte>(), headers: headers);

                            status = tpl.responseHeaders.TryGetValue(X_Goog_Upload_Status);
                            _logger.LogTrace($"status={status}");
                            var bytesReceived = tpl.responseHeaders.TryGetValue(X_Goog_Upload_Size_Received);
                            //Debug.WriteLine($"bytesReceived={bytesReceived}");
                            Debug.WriteLine($"attemptCount={attemptCount}\twill try upload again...");
                        }
                        else
                        {
                            attemptCount = 0;//reset retry count
                            offset += bytes.Length;
                            RaiseUploadProgressEvent(new UploadProgressArgs(Path.GetFileName(path), size, batchIndex, offset, bytes.Length));
                            batchIndex++;
                            //if (callback is object)
                            //    callback(bytes.Length);
                            //if (bytes.Length < Upload_Chunk_Granularity)
                            //    break;//this was the last one
                            if (lastChunk)
                                break;//this was the last one
                        }
                    }
                    return tpl.result;
                }
                else
                    throw new NotSupportedException($"not supported upload type '{uploadMethod}'");
            }

            string GetMimeType()
            {
                var fileExtension = Path.GetExtension(path);
                if (string.IsNullOrWhiteSpace(fileExtension)) throw new NotSupportedException($"Missing file extension, unable to determine mime type for; {path}");
                if (IsImage(fileExtension))
                    return MimeTypeMap.GetMimeType(fileExtension);
                else if (IsVideo(fileExtension))
                    return MimeTypeMap.GetMimeType(fileExtension);
                else
                    throw new NotSupportedException($"Cannot match file extension '{fileExtension}' from '{path}' to a known image or video mime type.");
            }
        }

        static AlbumPosition? GetAlbumPosition(string? albumId, GooglePhotosPositionType positionType, string? relativeMediaItemId, string? relativeEnrichmentItemId)
        {
            AlbumPosition? albumPosition = null;
            if (string.IsNullOrWhiteSpace(albumId)
                && (positionType != GooglePhotosPositionType.LAST_IN_ALBUM || !string.IsNullOrWhiteSpace(relativeMediaItemId) || !string.IsNullOrWhiteSpace(relativeEnrichmentItemId)))
                throw new NotSupportedException($"cannot specify position without including an {nameof(albumId)}!");
            if (!string.IsNullOrWhiteSpace(relativeMediaItemId) && !string.IsNullOrWhiteSpace(relativeEnrichmentItemId))
                throw new NotSupportedException($"cannot specify {nameof(relativeMediaItemId)} and {nameof(relativeEnrichmentItemId)} at the same time!");
            if (positionType == GooglePhotosPositionType.LAST_IN_ALBUM || positionType == GooglePhotosPositionType.POSITION_TYPE_UNSPECIFIED)
            {
                //the default so ignore
            }
            else if (positionType == GooglePhotosPositionType.FIRST_IN_ALBUM)
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