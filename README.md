# CasCap.Apis.GooglePhotos
## *Unofficial* Google Photos REST API Wrapper for .NET

[azdo-badge]: https://dev.azure.com/f2calv/github/_apis/build/status/f2calv.CasCap.Apis.GooglePhotos?branchName=master
[azdo-url]: https://dev.azure.com/f2calv/github/_build/latest?definitionId=7&branchName=master
[azdo-coverage-url]: https://img.shields.io/azure-devops/coverage/f2calv/github/7
[CasCap.Apis.GooglePhotos-badge]: https://img.shields.io/nuget/v/CasCap.Apis.GooglePhotos?color=blue
[CasCap.Apis.GooglePhotos-url]: https://nuget.org/packages/CasCap.Apis.GooglePhotos

[![Build Status][azdo-badge]][azdo-url] ![Code Coverage][azdo-coverage-url] [![Nuget][CasCap.Apis.GooglePhotos-badge]][CasCap.Apis.GooglePhotos-url]

This is an *unofficial* Google Photos REST API library targeting .NET Standard 2.0.

If you wish to interact with your Google Photos media items/albums then there are official [PHP and Java Client Libraries](https://developers.google.com/photos/library/guides/client-libraries). However if you're looking for an official .NET library then there are no options... 

The *CasCap.Apis.GooglePhotos* library wraps up all the available functionality of the Google Photos REST API in easy to consume method calls.

Note: Before you jump in and use this library you should be aware that the Google Photos API has some key limitations. The biggest of these is that the API only allows the upload/addition of images/videos to the library, no edits or deletion are possible and have to be done manually via https://photos.google.com.

## Google Photos API Set-up

When you create your photos application, you must first create an OAuth login details using the [Google API Console](https://console.developers.google.com/) and retrieve a Client ID and a Client Secret. Using your Google Account the steps to do this are as follows;

1. Visit [Google API Console](https://console.developers.google.com/)
2. Select 'Library' on the main menu;
  - Search for 'Photos Library API', select it from the results and hit the Enable button.
3. Select 'Credentials' on the main menu;
  - Select 'Create Credentials' on the sub menu and pick 'OAuth client ID'
  - Select 'Desktop' as the application type.
  - Enter a suitable application name and hit the Create button.
  - Copy/save the Client ID and Client Secret which are then displayed you will use these to authenticate with the GooglePhotosService.


## CasCap.Apis.GooglePhotos Set-up/Configuration

Install the package into your project using NuGet ([see details here](https://www.nuget.org/packages/CasCap.Apis.GooglePhotos/)).

For .NET Core applications using Dependency Injection the primary API usage is to call IServiceCollection.AddGooglePhotos in the Startup.cs ConfigureServices method.

```csharp
//startup.cs
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddGooglePhotos();
    }
}
```

There are 4 mandatory configuration options that must be passed;

- User (your email address)
- Google Client ID
- Google Client Secret
- Security Scopes

The recommended method of setting these options would be via the appsettings.json file;

```json5
// appsettings.json
{
    ...
    "CasCap": {
        "GooglePhotosOptions": {
            // This is the email address of the Google Account.
            "User": "your.email@mydomain.com",

            // There are 5 security scopes which can be combined.
            "Scopes": [
                "ReadOnly"
                //"AppendOnly",
                //"AppCreatedData",
                //"Access",
                //"Sharing"
                ],

            // The ClientId and ClientSecret are provided by the Google Console after you register your own application.
            "ClientId": "012345678901-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.apps.googleusercontent.com",
            "ClientSecret": "abcabcabcabcabcabcabcabc",
        }
    }
    ...
}
```

Alternatively you can pass the options into the AddGooglePhotos method;

```csharp
//startup.cs
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddGooglePhotos(options =>
        {
            options.User = "your.email@mydomain.com";
            options.Scopes = new[] { Scopes.ReadOnly };
            options.ClientId = "012345678901-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.apps.googleusercontent.com";
            options.ClientSecret = "abcabcabcabcabcabcabcabc";
        });
    }
}
```

The appsettings.json is the preferred option due to it's flexibility however the Client ID & Client Secret should be stored securely outside of source control i.e. [.NET Secret Manager](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-3.1&tabs=windows#secret-manager), [Azure KeyVault](https://azure.microsoft.com/en-us/services/key-vault/) or some equivalent.

If you don't use Dependency Injection you can new-up the GooglePhotosService manually and pass the configuration options, logger and HttpClient via the constructor;

```csharp
//MyPhotosClass.cs
using CasCap.Models;
using CasCap.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

public class MyPhotosClass
{
    public async Task Login_And_List_Albums()
    {
        //new-up logging
        var logger = new LoggerFactory().CreateLogger<GooglePhotosService>();

        //new-up configuration options
        var options = new GooglePhotosOptions
        {
            User = "your.email@mydomain.com",//**replace with your info**
            ClientId = "012345678901-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.apps.googleusercontent.com",//**replace with your info**
            ClientSecret = "abcabcabcabcabcabcabcabc",//**replace with your info**
            Scopes = new[] { GooglePhotos.Scope.ReadOnly },
        };

        //new-up a single HttpClient
        var handler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate };
        var client = new HttpClient(handler) { BaseAddress = new Uri(options.BaseAddress) };

        //new-up the GooglePhotosService and pass in the logger, options and HttpClient
        var googlePhotosSvc = new GooglePhotosService(logger, Options.Create(options), client);

        //attempt to log-in
        if (!await googlePhotosSvc.Login())
            throw new Exception($"login failed!");

        //get and list all albums
        var albums = await googlePhotosSvc.GetAlbums();
        foreach (var album in albums)
        {
            Console.WriteLine(album.title);
        }
    }
}
```



### Authentication

[OAuth 2.0 authentication](https://developers.google.com/identity/protocols/oauth2) is handled by the official [Google.Apis.Auth](https://www.nuget.org/packages/Google.Apis.Auth/) NuGet package, for more information on this see the project site for [Google APIs client Library for .NET](https://github.com/googleapis/google-api-dotnet-client).

There are 5 [authorisation scopes](https://developers.google.com/photos/library/guides/authorization) to choose which designate the level of access you wish to give to the photos library;

- ReadOnly
- AppendOnly
- AppCreatedData
- Access
- Sharing

Note: If you wish to give your application unfettered access to your photos then use both the Access and Sharing scopes.

#### FileDataStoreFullPath

The [Google.Apis.Auth](https://www.nuget.org/packages/Google.Apis.Auth/) library will cache the OAuth 2.0 login information in a local JSON file which it will then read tokens from and renew if necessary on subsequent logins. The JSON file(s) are stored per-User at Environment.SpecialFolder.ApplicationData;

- Windows, %user%/something
- Linux ?
- Mac ?

If you change authentication scopes or User you should look for and delete the original file and allow the [Google.Apis.Auth](https://www.nuget.org/packages/Google.Apis.Auth/) library to re-create a new JSON file with the new scopes.

### Core Methods

All API functions are exposed by the GoogleServices class
todo: insert table of key methods?
todo: self-documenting XML comments?

### Sample Projects

These are demonstration .NET Core applications;

- [Simple Console App](https://github.com/f2calv/CasCap.Apis.GooglePhotos/tree/master/samples/ConsoleApp) not using Dependency Injection.
- [Advanced Console App](https://github.com/f2calv/CasCap.Apis.GooglePhotos/tree/master/samples/GenericHost) using Configuration, Logging and Dependency Injection via the [].NET Generic Host](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-3.1).


### Misc

### Dependencies

Key dependencies of this library;

- [Google.Apis.Auth](https://www.nuget.org/packages/Google.Apis.Auth/) handles all authentication.
- [CasCap.Common.Net](https://www.nuget.org/packages/CasCap.Common.Net/) handles all HttpClient requests.
[see details here](https://www.nuget.org/packages/CasCap.Apis.GooglePhotos/)
  - Polly

### Resources

- https://developers.google.com/photos

### Roadmap

- Create interface that works with other image/media services?
