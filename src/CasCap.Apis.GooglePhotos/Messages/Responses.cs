using CasCap.Interfaces;
using CasCap.Models;
using System.Collections.Generic;
namespace CasCap.Messages;

internal class albumsGetResponse : ResponseBase
{
    public List<Album>? albums { get; set; }
    public List<Album>? sharedAlbums { get; set; }
}

internal class sharedAlbumResponse
{
    public ShareInfo shareInfo { get; set; } = default!;
}

public class mediaItemsCreateResponse//todo: should this be internal?
{
    /// <summary>
    /// Output only. List of media items created.
    /// </summary>
    public List<NewMediaItemResult> newMediaItemResults { get; set; } = default!;
}

/// <summary>
/// https://developers.google.com/photos/library/reference/rest/v1/albums/addEnrichment#request-body
/// </summary>
internal class AddEnrichmentRequest
{
    public AddEnrichmentRequest(NewEnrichmentItem newEnrichmentItem, AlbumPosition albumPosition)
    {
        this.newEnrichmentItem = newEnrichmentItem;
        this.albumPosition = albumPosition;
    }

    /// <summary>
    /// The enrichment to be added.
    /// </summary>
    public NewEnrichmentItem newEnrichmentItem { get; set; }

    /// <summary>
    /// The position in the album where the enrichment is to be inserted.
    /// </summary>
    public AlbumPosition albumPosition { get; set; }
}

/// <summary>
/// https://developers.google.com/photos/library/reference/rest/v1/albums/addEnrichment#response-body
/// </summary>
internal class AddEnrichmentResponse
{
    /// <summary>
    /// Output only. Enrichment which was added.
    /// </summary>
    public enrichmentItem enrichmentItem { get; set; } = default!;
}

public class enrichmentItem
{
    public string id { get; set; } = default!;
}

internal class mediaItemsResponse : ResponseBase
{
    public List<MediaItem> mediaItems { get; set; } = default!;
}

internal class mediaItemsGetResponse
{
    public List<mediaItemGetResponse> mediaItemResults { get; set; } = default!;
}

internal class mediaItemGetResponse
{
    public MediaItem mediaItem { get; set; } = default!;
    public Status status { get; set; } = default!;
}

public abstract class ResponseBase : IPagingToken
{
    public string? nextPageToken { get; set; }
}

//https://developers.google.com/photos/library/reference/rest/v1/mediaItems/batchCreate
//public class mediaItemsCreateRequest
//{
//    public string albumId { get; set; }
//    public List<NewMediaItem> newMediaItems { get; set; }
//    public AlbumPosition albumPosition { get; set; }
//}