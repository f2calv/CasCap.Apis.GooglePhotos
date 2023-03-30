using CasCap.Models;
using CasCap.Services;
using CasCap.Xunit;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace CasCap.Apis.GooglePhotos.Tests;

public class ExifTests : TestBase
{
    public ExifTests(ITestOutputHelper output) : base(output) { }

    /// <summary>
    /// Minimal exif tags added by Google.
    /// </summary>
    const int googleExifTagCount = 5;

    [SkipIfCIBuildTheory, Trait("Type", nameof(GooglePhotosService))]
    [InlineData("test11.jpg", 55.041388888888889d, 8.4677777777777781d, 62)]
    public async Task CheckExifData(string fileName, double latitude, double longitude, int exifTagCount)
    {
        var path = $"{_testFolder}{fileName}";
        var originalBytes = File.ReadAllBytes(path);

        var loginResult = await _googlePhotosSvc.LoginAsync();
        Assert.True(loginResult);

        var tplOriginal = await ExifTests.GetExifInfo(path);
        Assert.Equal(latitude, tplOriginal.latitude);
        Assert.Equal(longitude, tplOriginal.longitude);
        Assert.Equal(exifTagCount, tplOriginal.exifTagCount);

        var uploadToken = await _googlePhotosSvc.UploadMediaAsync(path, GooglePhotosUploadMethod.Simple);
        Assert.NotNull(uploadToken);
        var newMediaItemResult = await _googlePhotosSvc.AddMediaItemAsync(uploadToken, path);
        Assert.NotNull(newMediaItemResult);
        //the upload returns a null baseUrl
        Assert.Null(newMediaItemResult.mediaItem.baseUrl);

        //so now retrieve all media items
        var mediaItems = await _googlePhotosSvc.GetMediaItemsAsync().ToListAsync();

        var uploadedMediaItem = mediaItems.FirstOrDefault(p => p.filename.Equals(fileName));
        Assert.NotNull(uploadedMediaItem);
        Assert.True(uploadedMediaItem.isPhoto);

        var bytesNoExif = await _googlePhotosSvc.DownloadBytes(uploadedMediaItem, includeExifMetadata: false);
        Assert.NotNull(bytesNoExif);
        var tplNoExif = await ExifTests.GetExifInfo(bytesNoExif);
        Assert.True(googleExifTagCount == tplNoExif.exifTagCount);

        var bytesWithExif = await _googlePhotosSvc.DownloadBytes(uploadedMediaItem, includeExifMetadata: true);
        Assert.NotNull(bytesWithExif);
        var tplWithExif = await ExifTests.GetExifInfo(bytesWithExif);
        Assert.Null(tplWithExif.latitude);//location exif data always stripped :(
        Assert.Null(tplWithExif.longitude);//location exif data always stripped :(
        Assert.True(tplOriginal.exifTagCount > tplWithExif.exifTagCount);//due to Google-stripping fewer exif tags are returned
        Assert.True(googleExifTagCount < tplWithExif.exifTagCount);
    }

    static async Task<(double? latitude, double? longitude, int exifTagCount)> GetExifInfo(string path)
    {
        using var image = await Image.LoadAsync(path);
        return GetLatLong(image);
    }

    static async Task<(double? latitude, double? longitude, int exifTagCount)> GetExifInfo(byte[] bytes)
    {
        var stream = new MemoryStream(bytes);
        using var image = await Image.LoadAsync(stream);
        return GetLatLong(image);
    }

    static (double? latitude, double? longitude, int exifTagCount) GetLatLong(Image image)
    {
        double? latitude = null, longitude = null;
        var exifTagCount = image.Metadata.ExifProfile?.Values.Count ?? 0;
        if (image.Metadata.ExifProfile.Values?.Any() ?? false)
        {
            var exifData = image.Metadata.ExifProfile;
            if (exifData != null)
            {
                if (exifData.TryGetValue(ExifTag.GPSLatitude, out var gpsLatitude)
                    && exifData.TryGetValue(ExifTag.GPSLatitudeRef, out var gpsLatitudeRef))
                    latitude = GetCoordinates(gpsLatitudeRef.ToString(), gpsLatitude.Value);

                if (exifData.TryGetValue(ExifTag.GPSLongitude, out var gpsLong)
                    && exifData.TryGetValue(ExifTag.GPSLongitudeRef, out var gpsLongRef))
                    longitude = GetCoordinates(gpsLongRef.ToString(), gpsLong.Value);

                Debug.WriteLine($"latitude,longitude = {latitude},{longitude}");
            }
        }

        return (latitude, longitude, exifTagCount);
    }

    static double GetCoordinates(string gpsRef, Rational[] rationals)
    {
        var degrees = rationals[0].Numerator / rationals[0].Denominator;
        var minutes = rationals[1].Numerator / rationals[1].Denominator;
        var seconds = rationals[2].Numerator / rationals[2].Denominator;

        var coordinate = degrees + (minutes / 60d) + (seconds / 3600d);
        if (gpsRef == "S" || gpsRef == "W")
            coordinate *= -1;
        return coordinate;
    }
}