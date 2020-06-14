using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
namespace CasCap.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum GooglePhotosScope
    {
        /// <summary>
        /// Read access only.
        /// 
        /// List items from the library and all albums, access all media items and list albums owned by the user, including those which have been shared with them.
        ///
        /// For albums shared by the user, share properties are only returned if the.sharing scope has also been granted.
        ///
        /// The ShareInfo property for albums and the contributorInfo for mediaItems is only available if the.sharing scope has also been granted.
        /// </summary>
        ReadOnly,

        /// <summary>
        /// Write access only.
        ///
        /// Access to upload bytes, create media items, create albums, and add enrichments.Only allows new media to be created in the user's library and in albums created by the app.
        /// </summary>
        AppendOnly,

        /// <summary>
        /// Read access to media items and albums created by the developer. For more information, see Access media items and List library contents, albums, and media items.
        ///
        /// Intended to be requested together with the.appendonly scope.
        /// </summary>
        AppCreatedData,

        /// <summary>
        /// Access to both the .appendonly and .readonly scopes. Doesn't include .sharing.
        /// </summary>
        Access,

        /// <summary>
        /// Access to sharing calls.
        ///
        /// Access to create an album, share it, upload media items to it, and join a shared album.
        /// </summary>   
        Sharing
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum GooglePhotosUploadMethod
    {
        Simple,
        ResumableSingle,
        ResumableMultipart
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum GooglePhotosPositionType
    {
        /// <summary>
        /// Default value if this enum isn't set.
        /// </summary>
        POSITION_TYPE_UNSPECIFIED,

        /// <summary>
        /// At the beginning of the album.
        /// </summary>
        FIRST_IN_ALBUM,

        /// <summary>
        /// At the end of the album.
        /// </summary>
        LAST_IN_ALBUM,

        /// <summary>
        /// After a media item.
        /// </summary>
        AFTER_MEDIA_ITEM,

        /// <summary>
        /// After an enrichment item.
        /// </summary>
        AFTER_ENRICHMENT_ITEM
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum GooglePhotosMediaType
    {
        PHOTO,
        VIDEO
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum GooglePhotosFeatureType
    {
        FAVORITES
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum GooglePhotosContentCategoryType
    {
        ANIMALS,
        ARTS,
        BIRTHDAYS,
        CITYSCAPES,
        CRAFTS,
        DOCUMENTS,
        FASHION,
        FLOWERS,
        FOOD,
        GARDENS,
        HOLIDAYS,
        HOUSES,
        LANDMARKS,
        LANDSCAPES,
        NIGHT,
        PEOPLE,
        PERFORMANCES,
        PETS,
        RECEIPTS,
        SCREENSHOTS,
        SELFIES,
        SPORT,
        TRAVEL,
        UTILITY,
        WEDDINGS,
        WHITEBOARDS
    }

    public class Filter
    {
        public Filter() { }

        public Filter(DateTime startDate, DateTime endDate)
        {
            dateFilter = new dateFilter
            {
                ranges = new[] { new range { startDate = new date(startDate), endDate = new date(endDate) } }
            };
        }

        public Filter(GooglePhotosContentCategoryType category) => this.contentFilter = new contentFilter { includedContentCategories = new[] { category } };

        public Filter(GooglePhotosContentCategoryType[] categories) => this.contentFilter = new contentFilter { includedContentCategories = categories };

        public Filter(List<GooglePhotosContentCategoryType> categories) => this.contentFilter = new contentFilter { includedContentCategories = categories.ToArray() };

        public contentFilter? contentFilter { get; set; }
        public dateFilter? dateFilter { get; set; }
        public featureFilter? featureFilter { get; set; }
        public mediaTypeFilter? mediaTypeFilter { get; set; }
        public bool excludeNonAppCreatedData { get; set; }
        public bool includeArchivedMedia { get; set; }
    }

    public class contentFilter
    {
        public GooglePhotosContentCategoryType[]? includedContentCategories { get; set; }
        public GooglePhotosContentCategoryType[]? excludedContentCategories { get; set; }
    }

    public class dateFilter
    {
        public date[]? dates { get; set; }
        public range[]? ranges { get; set; }
    }

    public class featureFilter
    {
        public GooglePhotosFeatureType[] includedFeatures { get; set; } = default!;
    }

    public class mediaTypeFilter
    {
        public GooglePhotosMediaType[] mediaTypes { get; set; } = default!;
    }

    public class date
    {
        public date(DateTime dt)
        {
            this.month = dt.Month;
            this.day = dt.Day;
            this.year = dt.Year;
        }

        public date(int month, int day, int year)
        {
            this.month = month;
            this.day = day;
            this.year = year;
        }

        public int month { get; }
        public int day { get; }
        public int year { get; }
    }

    public class range
    {
        public date startDate { get; set; } = default!;
        public date endDate { get; set; } = default!;
    }

    public class Album
    {
        /// <summary>
        /// Identifier for the album. This is a persistent identifier that can be used between sessions to identify this album.
        /// </summary>
        public string id { get; set; } = default!;

        /// <summary>
        /// Name of the album displayed to the user in their Google Photos account. This string shouldn't be more than 500 characters.
        /// </summary>
        public string title { get; set; } = default!;

        /// <summary>
        /// [Output only] Google Photos URL for the album. The user needs to be signed in to their Google Photos account to access this link.
        /// </summary>
        public string productUrl { get; set; } = default!;

        /// <summary>
        /// [Output only] A URL to the cover photo's bytes. This shouldn't be used as is. Parameters should be appended to this URL before use. See the developer documentation for a complete list of supported parameters. For example, '=w2048-h1024' sets the dimensions of the cover photo to have a width of 2048 px and height of 1024 px.
        /// </summary>
        public string coverPhotoBaseUrl { get; set; } = default!;

        /// <summary>
        /// [Output only] Identifier for the media item associated with the cover photo.
        /// </summary>
        public string coverPhotoMediaItemId { get; set; } = default!;

        /// <summary>
        /// [Output only] True if you can create media items in this album. This field is based on the scopes granted and permissions of the album. If the scopes are changed or permissions of the album are changed, this field is updated.
        /// </summary>
        public bool isWriteable { get; set; }

        /// <summary>
        /// [Output only] The number of media items in the album.
        /// </summary>
        public int mediaItemsCount { get; set; }

        /// <summary>
        /// [Output only] Information related to shared albums.This field is only populated if the album is a shared album, the developer created the album and the user has granted the photoslibrary.sharing scope.
        /// </summary>
        public ShareInfo? shareInfo { get; set; }

        public override string ToString()
        {
            return $"{title}, {mediaItemsCount} media items";
        }
    }

    public class ShareInfo
    {
        /// <summary>
        /// Options that control the sharing of an album.
        /// </summary>
        public SharedAlbumOptions sharedAlbumOptions { get; set; } = default!;

        /// <summary>
        /// A link to the album that's now shared on the Google Photos website and app. Anyone with the link can access this shared album and see all of the items present in the album.
        /// </summary>
        public string shareableUrl { get; set; } = default!;

        /// <summary>
        /// A token that can be used by other users to join this shared album via the API.
        /// </summary>
        public string shareToken { get; set; } = default!;

        /// <summary>
        /// True if the user has joined the album. This is always true for the owner of the shared album.
        /// </summary>
        public bool isJoined { get; set; }

        /// <summary>
        /// True if the user owns the album.
        /// </summary>
        public bool isOwned { get; set; }
    }

    public class SharedAlbumOptions
    {
        /// <summary>
        /// True if the shared album allows collaborators (users who have joined the album) to add media items to it. Defaults to false.
        /// </summary>
        public bool isCollaborative { get; set; }

        /// <summary>
        /// True if the shared album allows the owner and the collaborators (users who have joined the album) to add comments to the album. Defaults to false.
        /// </summary>
        public bool isCommentable { get; set; }
    }

    //https://developers.google.com/photos/library/reference/rest/v1/mediaItems/batchCreate#NewMediaItemResult
    public class NewMediaItemResult
    {
        public string uploadToken { get; set; } = default!;
        public Status status { get; set; } = default!;
        public MediaItem mediaItem { get; set; } = default!;
    }

    //https://developers.google.com/photos/library/reference/rest/v1/Status
    public class Status
    {
        public int code { get; set; }
        public string? message { get; set; }
        public List<object>? details { get; set; }
    }

    public class NewMediaItem
    {
        /// <summary>
        /// Description of the media item. This will be shown to the user in the item's info section in the Google Photos app. This string shouldn't be more than 1000 characters.
        /// </summary>
        public string? description { get; set; }

        /// <summary>
        /// A new media item that has been uploaded via the included uploadToken.
        /// </summary>
        public SimpleMediaItem simpleMediaItem { get; set; } = default!;
    }

    public class UploadItem//my custom class to upload multiple items
    {
        public UploadItem(string uploadToken, string? fileName, string? description)
        {
            if (string.IsNullOrWhiteSpace(uploadToken)) throw new ArgumentException($"{nameof(uploadToken)} is null or whitespace??");
            this.uploadToken = uploadToken;
            this.fileName = Path.GetFileName(fileName);
            this.description = description;
        }

        public UploadItem(string uploadToken, string? fileName)
        {
            if (string.IsNullOrWhiteSpace(uploadToken)) throw new ArgumentException($"{nameof(uploadToken)} is null or whitespace??");
            this.uploadToken = uploadToken;
            this.fileName = Path.GetFileName(fileName);
        }

        public string uploadToken { get; }
        public string? fileName { get; }
        public string? description { get; }
    }

    /// <summary>
    /// https://developers.google.com/photos/library/reference/rest/v1/mediaItems/batchCreate#SimpleMediaItem
    /// </summary>
    public class SimpleMediaItem
    {
        /// <summary>
        /// Token identifying the media bytes that have been uploaded to Google.
        /// </summary>
        public string uploadToken { get; set; } = default!;

        /// <summary>
        /// File name with extension of the media item. This is shown to the user in Google Photos. The file name specified during the byte upload process is ignored if this field is set. The file name, including the file extension, shouldn't be more than 255 characters. This is an optional field.
        /// </summary>
        public string? fileName { get; set; }
    }

    //https://developers.google.com/photos/library/reference/rest/v1/AlbumPosition
    public class AlbumPosition
    {
        public GooglePhotosPositionType position { get; set; }

        // Union field relative_item can be only one of the following:
        public string? relativeMediaItemId { get; set; }

        public string? relativeEnrichmentItemId { get; set; }
        // End of list of possible types for union field relative_item.
    }

    #region enrichments
    /// <summary>
    /// A new enrichment item to be added to an album, used by the albums.addEnrichment call.
    /// Note: Only one property can be set.
    /// </summary>
    public class NewEnrichmentItem
    {
        public NewEnrichmentItem(string text)
        {
            textEnrichment = new TextEnrichment(text);
        }

        public NewEnrichmentItem(string locationName, double latitude, double longitude)
        {
            locationEnrichment = new LocationEnrichment(new Location(locationName, new Latlng(latitude, longitude)));
        }

        public NewEnrichmentItem(Location origin, Location destination)
        {
            mapEnrichment = new MapEnrichment(origin, destination);
        }

        /// <summary>
        /// Text to be added to the album.
        /// </summary>
        public TextEnrichment? textEnrichment { get; set; }

        /// <summary>
        /// Location to be added to the album.
        /// </summary>
        public LocationEnrichment? locationEnrichment { get; set; }

        /// <summary>
        /// Map to be added to the album.
        /// </summary>
        public MapEnrichment? mapEnrichment { get; set; }
    }

    /// <summary>
    /// An enrichment item.
    /// https://developers.google.com/photos/library/reference/rest/v1/albums/addEnrichment#enrichmentitem
    /// </summary>
    public class EnrichmentItem
    {
        /// <summary>
        /// Identifier of the enrichment item.
        /// </summary>
        public string id { get; set; } = default!;
    }

    /// <summary>
    /// Text for this enrichment item.
    /// https://developers.google.com/photos/library/reference/rest/v1/albums/addEnrichment#textenrichment
    /// </summary>
    public class TextEnrichment
    {
        public TextEnrichment(string text)
        {
            this.text = text;
        }

        public string text { get; set; }
    }

    /// <summary>
    /// An enrichment containing a single location.
    /// https://developers.google.com/photos/library/reference/rest/v1/albums/addEnrichment#locationenrichment
    /// </summary>
    public class LocationEnrichment
    {
        public LocationEnrichment(Location location)
        {
            this.location = location;
        }

        /// <summary>
        /// Location for this enrichment item.
        /// </summary>
        public Location location { get; set; }
    }

    /// <summary>
    /// Represents a physical location.
    /// https://developers.google.com/photos/library/reference/rest/v1/albums/addEnrichment#location
    /// </summary>
    public class Location
    {
        public Location(string locationName, Latlng latlng)
        {
            this.locationName = locationName;
            this.latlng = latlng;
        }

        /// <summary>
        /// Name of the location to be displayed.
        /// </summary>
        public string locationName { get; set; }

        /// <summary>
        /// Position of the location on the map.
        /// </summary>
        public Latlng latlng { get; set; }
    }

    /// <summary>
    /// An object representing a latitude/longitude pair. This is expressed as a pair of doubles representing degrees latitude and degrees longitude. Unless specified otherwise, this must conform to the WGS84 standard. Values must be within normalized ranges.
    /// https://developers.google.com/photos/library/reference/rest/v1/albums/addEnrichment#latlng
    /// </summary>
    public class Latlng
    {
        public Latlng(double latitude, double longitude)
        {
            this.latitude = latitude;
            this.longitude = longitude;
        }

        /// <summary>
        /// The latitude in degrees. It must be in the range [-90.0, +90.0].
        /// </summary>
        public double latitude { get; set; }

        /// <summary>
        /// The longitude in degrees. It must be in the range [-180.0, +180.0].
        /// </summary>
        public double longitude { get; set; }
    }

    /// <summary>
    /// An enrichment containing a map, showing origin and destination locations.
    /// https://developers.google.com/photos/library/reference/rest/v1/albums/addEnrichment#mapenrichment
    /// </summary>
    public class MapEnrichment
    {
        public MapEnrichment(Location origin, Location destination)
        {
            this.origin = origin;
            this.destination = destination;
        }

        /// <summary>
        /// Origin location for this enrichment item.
        /// </summary>
        public Location origin { get; set; }

        /// <summary>
        /// Destination location for this enrichment item.
        /// </summary>
        public Location destination { get; set; }
    }
    #endregion

    //https://developers.google.com/photos/library/guides/access-media-items#media-items
    public class MediaItem
    {
        /// <summary>
        /// A permanent, stable ID used to identify the object.
        /// </summary>
        public string id { get; set; } = default!;

        /// <summary>
        /// Description of the media item as seen inside Google Photos.
        /// </summary>
        public string? description { get; set; }

        /// <summary>
        /// A link to the image inside Google Photos. This link can't be opened by the developer, only by the user.
        /// </summary>
        public string productUrl { get; set; } = default!;

        /// <summary>
        /// Used to access the raw bytes. For more information, see Base URLs.
        /// </summary>
        public string baseUrl { get; set; } = default!;

        /// <summary>
        /// The type of the media item to help easily identify the type of media (for example: image/jpg).
        /// </summary>
        public string mimeType { get; set; } = default!;//todo: nullability look further into this (will it return a mime type if we don't send one in?)

        /// <summary>
        /// Varies depending on the underlying type of the media, such as, photo or video. To reduce the payload, field masks can be used.
        /// </summary>
        public MediaMetaData mediaMetadata { get; set; } = default!;

        /// <summary>
        /// The filename of the media item shown to the user in the Google Photos app (within the item's info section).
        /// </summary>
        public string filename { get; set; } = default!;

        /// <summary>
        /// This field is only populated if the media item is in a shared album created by this app and the user has granted the .sharing scope.
        ///
        /// Contains information about the contributor who added this media item. For more details, see Share media.
        /// </summary>
        public ContributorInfo? contributorInfo { get; set; }

        public override string ToString()
        {
            return $"{this.filename} {this.mediaMetadata.creationTime:yyyy-MM-dd HH:mm:ss}";
        }
    }

    public class MediaMetaData
    {
        public DateTime creationTime { get; set; }
        public string width { get; set; } = default!;
        public string height { get; set; } = default!;
        public Photo? photo { get; set; }
        public Video? video { get; set; }
    }

    public class Photo
    {
        public string? cameraMake { get; set; }
        public string? cameraModel { get; set; }
        public float focalLength { get; set; }
        public float apertureFNumber { get; set; }
        public int isoEquivalent { get; set; }
        public float exposureTime { get; set; }
    }

    public class ContributorInfo
    {
        public string? profilePictureBaseUrl { get; set; }
        public string? displayName { get; set; }
    }

    public class Video
    {
        public double fps { get; set; }
        public string status { get; set; } = default!;
        public string? cameraMake { get; set; }
        public string? cameraModel { get; set; }
    }
}