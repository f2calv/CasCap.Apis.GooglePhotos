namespace CasCap.Models
{
    public class RequestUris
    {
        public const string BaseAddress = "https://photoslibrary.googleapis.com/v1/";

        /// <summary>
        /// POST Adds an enrichment at a specified position in a defined album.
        /// </summary>
        public const string POST_albums_addEnrichment = "albums/{0}:addEnrichment";

        /// <summary>
        /// POST Adds one or more media items in a user's Google Photos library to an album.
        /// </summary>
        public const string POST_albums_batchAddMediaItems = "albums/{0}:batchAddMediaItems";

        /// <summary>
        /// POST Removes one or more media items from a specified album.
        /// </summary>
        public const string POST_albums_batchRemoveMediaItems = "albums/{0}:batchRemoveMediaItems";

        /// <summary>
        /// POST Creates an album in a user's Google Photos library.
        /// </summary>
        public const string GET_albums = "albums";

        /// <summary>
        /// GET Lists all albums shown to a user in the Albums tab of the Google Photos app.
        /// </summary>
        public const string POST_albums = "albums";

        /// <summary>
        /// Returns the album based on the specified albumId.
        /// </summary>
        public const string GET_album = "albums/{0}";

        /// <summary>
        /// Marks an album as shared and accessible to other users.
        /// </summary>
        public const string POST_share = "albums/{0}:share";

        /// <summary>
        /// Marks a previously shared album as private.
        /// </summary>
        public const string POST_unshare = "albums/{0}:unshare";

        public const string uploads = nameof(uploads);
        public const string GET_mediaItems = "mediaItems";
        public const string POST_mediaItems_search = "mediaItems:search";
        public const string POST_mediaItems_batchCreate = "mediaItems:batchCreate";
        public const string GET_mediaItems_batchGet = "mediaItems:batchGet";
        public const string GET_sharedAlbums = "sharedAlbums";
        public const string GET_sharedAlbum = "sharedAlbums/{0}";
        public const string POST_sharedAlbums_join = "sharedAlbums:join";
        public const string POST_sharedAlbums_leave = "sharedAlbums:leave";
    }
}