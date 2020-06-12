using System;
using System.IO;
namespace CasCap.Models
{
    [Serializable]
    public class GooglePhotosOptions
    {
        /// <summary>
        /// The default endpoint for REST API requests, currently defaults to REST API v1.0
        /// </summary>
        public string BaseAddress { get; set; } = RequestUris.BaseAddress;

        /// <summary>
        /// The email address of the Google Account that holds the photos.
        /// e.g. your.email@mydomain.com
        /// </summary>
        public string User { get; set; } = default!;

        /// <summary>
        /// Security Scopes, i.e. access levels.
        /// Note: When changing scopes under the same User you must manually delete the local JSON file to clear the local cache,
        /// you can use the GooglePhotosOptions.FileDataStoreFullPathDefault property to locate the path to the JSON file(s).
        /// </summary>
        public GooglePhotosScope[] Scopes { get; set; } = default!;

        /// <summary>
        /// Google Client Id string, numerical/alphanumeric.
        /// e.g. 012345678901-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.apps.googleusercontent.com
        /// </summary>
        public string ClientId { get; set; } = default!;

        /// <summary>
        /// Google Client Secret string, alphabetical.
        /// i.e. abcabcabcabcabcabcabcabc
        /// </summary>
        public string ClientSecret { get; set; } = default!;

        /// <summary>
        /// Folder path to locally cached OAuth 2.0 JSON file.
        /// If FileDataStoreFullPathOverride is left as null then Google Auth library will use the
        /// FileDataStoreFullPathDefault location to store a cached OAuth 2.0 JSON file per User.
        /// </summary>
        public string? FileDataStoreFullPathOverride { get; set; }

        /// <summary>
        /// e.g. Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Google.Apis.Auth");
        /// </summary>
        public string FileDataStoreFullPathDefault { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Google.Apis.Auth"); } }
    }
}