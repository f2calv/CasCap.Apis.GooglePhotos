using CasCap.Services;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
namespace CasCap.Apis.GooglePhotos.Tests
{
    /// <summary>
    /// Holds the integration tests for the GooglePhotos.
    /// Update appsettings.json with appropriate login values before running.
    /// </summary>
    public class Tests : TestBase
    {
        [Theory, Trait("Type", nameof(GooglePhotosService))]
        [InlineData("test1a", "test1b")]
        //[InlineData("test2a", "test2b")]
        public async Task Login(string title, string title2)
        {
            Debug.WriteLine(title);
            Debug.WriteLine(title2);
            var loginResult = await _googlePhotosSvc.LoginAsync();
            Assert.True(loginResult);

            //var albums = await _googlePhotosSvc.GetAlbums();
            //Assert.NotNull(albums);
            //if (!albums.IsNullOrEmpty())
            //{
            //    if (albums.Any(p => p.title == title))
            //        throw new Exception("manually delete the test albums and re-run the test...");
            //}
            //var strAlbum = await _googlePhotosSvc.CreateAlbum<string>(title);
            //Assert.NotNull(strAlbum);
            //var album = strAlbum.FromJSON<Album>();
            //Assert.NotNull(album);
            //Assert.True(album.title == title);

            //var album2 = await _googlePhotosSvc.CreateAlbum<Album>(title2);
            //Assert.NotNull(album2);
            //Assert.True(album2.title == title2);
        }

        [Fact]
        public async Task EndToEnd()
        {
            var loginResult = await _googlePhotosSvc.LoginAsync();
            Assert.True(loginResult);

        }
    }
}