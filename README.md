# CasCap.Apis.GooglePhotos

## _Unofficial_ Google Photos Library API wrapper library for .NET applications

[azdo-badge]: https://dev.azure.com/f2calv/github/_apis/build/status/f2calv.CasCap.Apis.GooglePhotos?branchName=master
[azdo-url]: https://dev.azure.com/f2calv/github/_build/latest?definitionId=7&branchName=master
[azdo-coverage-url]: https://img.shields.io/azure-devops/coverage/f2calv/github/7
[cascap.apis.googlephotos-badge]: https://img.shields.io/nuget/v/CasCap.Apis.GooglePhotos?color=blue
[cascap.apis.googlephotos-url]: https://nuget.org/packages/CasCap.Apis.GooglePhotos

![CI](https://github.com/f2calv/CasCap.Apis.GooglePhotos/actions/workflows/ci.yml/badge.svg) [![Coverage Status](https://coveralls.io/repos/github/f2calv/CasCap.Apis.GooglePhotos/badge.svg?branch=main)](https://coveralls.io/github/f2calv/CasCap.Apis.GooglePhotos?branch=main) [![SonarCloud Coverage](https://sonarcloud.io/api/project_badges/measure?project=f2calv_CasCap.Apis.GooglePhotos&metric=code_smells)](https://sonarcloud.io/component_measures/metric/code_smells/list?id=f2calv_CasCap.Apis.GooglePhotos) [![Nuget][cascap.apis.googlephotos-badge]][cascap.apis.googlephotos-url]

> Want to save yourself some coding? See the _preview_ release of [GooglePhotosCli](https://github.com/f2calv/CasCap.GooglePhotosCli) using this library...

This is an _unofficial_ Google Photos REST API library targeting .NET 6.0 & .NET 7.0.

Note: Older projects that require .NET Standard 2.0 please use version 1.x of this library.

If you find this library of use then please give it a thumbs-up by giving this repository a :star: ... :wink:

If you wish to interact with your Google Photos media items/albums then there are official [PHP and Java Client Libraries](https://developers.google.com/photos/library/guides/client-libraries). However if you're looking for a comprehensive .NET library then you were out of luck... until now :)

The _CasCap.Apis.GooglePhotos_ library wraps up all the available functionality of the Google Photos REST API in easy to use methods.

Note: Before you jump in and use this library you should be aware that the [Google Photos Library API](https://developers.google.com/photos/library/reference/rest) has some key limitations. The biggest of these is that the API only allows the upload/addition of images/videos to the library, no edits or deletion are possible and have to be done manually via [https://photos.google.com](https://photos.google.com).

## Google Photos API Set-up

When you create your photos application, you must first create an OAuth login details using the [Google API Console](https://console.developers.google.com/) and retrieve a Client ID and a Client Secret.

Using your Google Account the steps are\*;

1. Visit [Google API Console](https://console.developers.google.com/)
2. Select 'Library' on the main menu;
   - Search for 'Photos Library API', select it from the results and hit the Enable button.
3. Select 'Credentials' on the main menu;
   - Select 'Create Credentials' on the sub menu and pick 'OAuth client ID'
   - Select 'Desktop' as the application type.
   - Enter a suitable application name and hit the Create button.
   - Copy/save the Client ID and Client Secret which are then displayed you will use these to authenticate with the GooglePhotosService.

\*Note: the above instructions are correct as of 2022-04-06.

## Library Configuration/Usage

Install the package into your project using NuGet ([see details here](https://www.nuget.org/packages/CasCap.Apis.GooglePhotos/)).

For .NET Core applications using dependency injection the primary API usage is to call IServiceCollection.AddGooglePhotos in the Startup.cs ConfigureServices method.

```csharp
//Startup.cs
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
- Authorisation Scopes

There are 5 possible [authorisation scopes](https://developers.google.com/photos/library/guides/authorization) which designate the level of access you wish to give, these scopes can be combined if required;

- ReadOnly
- AppendOnly
- AppCreatedData
- Access
- Sharing

Best practise is to assign the lowest access level possible to meet your requirements. If you wish to go against best practise and give your application unfettered access to your media collection then use Access and Sharing scopes combined.

The recommended method of setting these mandatory options is via the appsettings.json file;

```json5
// appsettings.json
{
    ...
    "CasCap": {
        "GooglePhotosOptions": {
            // This is the email address of the Google Account.
            "User": "your.email@mydomain.com",

            // There are 5 security scopes, which can be combined.
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
//Startup.cs
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddGooglePhotos(options =>
        {
            options.User = "your.email@mydomain.com";//replace with **your** info
            options.ClientId = "012345678901-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.apps.googleusercontent.com";//replace with **your** info
            options.ClientSecret = "abcabcabcabcabcabcabcabc";//replace with **your** info
            options.Scopes = new[] { GooglePhotosScope.ReadOnly };
        });
    }
}
```

Using appsettings.json is generally the best option however remember the Client ID & Client Secret should be stored securely outside of source control via [Azure KeyVault](https://azure.microsoft.com/en-us/services/key-vault/) (or [.NET Secret Manager](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-3.1&tabs=windows#secret-manager) as shown below).

```pwsh
dotnet user-secrets init
dotnet user-secrets set "CasCap:GooglePhotosOptions:User" "your.email@mydomain.com" #replace with **your** info
dotnet user-secrets set "CasCap:GooglePhotosOptions:ClientId" "012345678901-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.apps.googleusercontent.com" #replace with **your** info
dotnet user-secrets set "CasCap:GooglePhotosOptions:ClientSecret" "abcabcabcabcabcabcabcabc" #replace with **your** info
```

After calling AddGooglePhotos in the ConfigureServices method of Startup.cs you can then call upon the GooglePhotosService within your own services shown below.

```csharp
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
namespace CasCap.Services
{
    public class MyPhotoService
    {
        readonly ILogger _logger;
        readonly GooglePhotosService _googlePhotosSvc;

        public MyPhotoService(ILogger<MyPhotoService> logger, GooglePhotosService googlePhotosSvc)
        {
            _logger = logger;
            _googlePhotosSvc = googlePhotosSvc;
        }

        public async Task Login_And_List_Albums()
        {
            if (!await _googlePhotosSvc.LoginAsync())
                throw new Exception($"login failed");

            var albums = await _googlePhotosSvc.GetAlbums();
            foreach (var album in albums)
            {
                _logger.LogInfo($"{album.id}\t{album.title}");
            }
        }
    }
}
```

If you don't use dependency injection you can new-up the GooglePhotosService manually and pass the mandatory configuration options, logger and HttpClient via the service constructor;

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
            User = "your.email@mydomain.com",//replace with **your** info
            ClientId = "012345678901-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.apps.googleusercontent.com",//replace with **your** info
            ClientSecret = "abcabcabcabcabcabcabcabc",//replace with **your** info
            Scopes = new[] { GooglePhotosScope.ReadOnly },
        };

        //new-up a single HttpClient
        var handler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate };
        var client = new HttpClient(handler) { BaseAddress = new Uri(options.BaseAddress) };

        //new-up the GooglePhotosService and pass in the logger, options and HttpClient
        var googlePhotosSvc = new GooglePhotosService(logger, Options.Create(options), client);

        //attempt to log-in
        if (!await googlePhotosSvc.LoginAsync())
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

## Misc

### Changing Users/Scopes

The [Google.Apis.Auth](https://www.nuget.org/packages/Google.Apis.Auth/) library will cache the OAuth 2.0 login information in a local JSON file which it will then read tokens from (and renew if necessary) on subsequent logins. The JSON file(s) are stored on a per-User basis in the Environment.SpecialFolder.ApplicationData folder. On Windows 10 this folder is located at;

- %UserProfile%\AppData\Roaming\Google.Apis.Auth

If you change the authentication scopes for a User you must delete the JSON file and allow the [Google.Apis.Auth](https://www.nuget.org/packages/Google.Apis.Auth/) library to re-auth and re-create a new JSON file with the new scopes.

You can change the location where these JSON token files are stored at using the FileDataStoreFullPathOverride property in the configuration options;

```csharp
//Startup.cs
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddGooglePhotos(options =>
        {
            options.User = "your.email@mydomain.com";
            options.Scopes = new[] { GooglePhotosScope.ReadOnly };
            options.ClientId = "012345678901-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.apps.googleusercontent.com";
            options.ClientSecret = "abcabcabcabcabcabcabcabc";
            //change FileDataStoreFullPathOverride
            options.FileDataStoreFullPathOverride = "c:/temp/GooglePhotos/"
        });
    }
}
```

### Sample Projects

All API functions are exposed by the GooglePhotosService class. There are several sample .NET Core applications which show the basics on how to set-up/config/use the library;

- [Console App](https://github.com/f2calv/CasCap.Apis.GooglePhotos/tree/master/samples/ConsoleApp) with no dependency injection.
- [Console App](https://github.com/f2calv/CasCap.Apis.GooglePhotos/tree/master/samples/GenericHost) using configuration, logging and dependency injection via the [.NET Generic Host](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-3.1).
- [Integration Test App](https://github.com/f2calv/CasCap.Apis.GooglePhotos/blob/master/src/CasCap.Apis.GooglePhotos.Tests/Tests/Tests.cs) has the majority of the commented examples of various interactions.

### Core Dependencies

- [Google.Apis.Auth](https://www.nuget.org/packages/Google.Apis.Auth/) handles the [OAuth 2.0 authentication](https://developers.google.com/identity/protocols/oauth2), see the [project site](https://github.com/googleapis/google-api-dotnet-client).
- [Polly](https://www.nuget.org/packages/Polly/) is a .NET resilience and transient-fault-handling library that handles retry, see [project site](https://github.com/App-vNext/Polly).
- [CasCap.Common.Extensions](https://www.nuget.org/packages/CasCap.Common.Extensions/) and [CasCap.Common.Net](https://www.nuget.org/packages/CasCap.Common.Net/) contains a variety of extension methods and abstract classes to make my life easier :)

### Misc Tips

- The [NuGet package](https://www.nuget.org/packages/CasCap.Apis.GooglePhotos/) includes [SourceLink](https://github.com/dotnet/sourcelink) which enables you to jump inside the library and debug the API yourself. By default Visual Studio 2017/2019 does not allow this and will pop up an message "You are debugging a Release build of...", to disable this message go into the Visual Studio debugging options and un-check the 'Just My Code' option (menu path, Tools > Options > Debugging).

### Resources

- https://developers.google.com/photos
- https://console.developers.google.com
- [Google Photos Library API](https://developers.google.com/photos)
- [Google Photos Library API REST Reference](https://developers.google.com/photos/library/reference/rest)
- [Google Photos Library API Authorisation Scopes](https://developers.google.com/photos/library/guides/authorization)

### Feedback/Issues

Please post any issues or feedback [here](https://github.com/f2calv/CasCap.Apis.GooglePhotos/issues).

### License

CasCap.Apis.GooglePhotos is Copyright &copy; 2020 [Alex Vincent](https://github.com/f2calv) under the [MIT license](LICENSE).
