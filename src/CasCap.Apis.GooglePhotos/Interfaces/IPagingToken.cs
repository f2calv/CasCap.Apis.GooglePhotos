namespace CasCap.Interfaces;

public interface IPagingToken
{
    /// <summary>
    /// A continuation token to get the next page of the results.
    /// </summary>
    string? nextPageToken { get; set; }
}