# CasCap.Apis.GooglePhotos
## *Unofficial* Google Photos REST API Wrapper for .NET

[azdo-badge]: https://dev.azure.com/f2calv/github/_apis/build/status/f2calv.CasCap.Apis.GooglePhotos?branchName=master
[azdo-url]: https://dev.azure.com/f2calv/github/_build/latest?definitionId=7&branchName=master
[azdo-coverage-url]: https://img.shields.io/azure-devops/coverage/f2calv/github/7
[CasCap.Apis.GooglePhotos-badge]: https://img.shields.io/nuget/v/CasCap.Apis.GooglePhotos?color=blue
[CasCap.Apis.GooglePhotos-url]: https://nuget.org/packages/CasCap.Apis.GooglePhotos

[![Build Status][azdo-badge]][azdo-url] ![Code Coverage][azdo-coverage-url] [![Nuget][CasCap.Apis.GooglePhotos-badge]][CasCap.Apis.GooglePhotos-url]

This is an *unofficial* Google Photos REST API wrapper library for .NET projects.

If you wish to interact with your Google Photos media items then there are official [PHP and Java Client Libraries](https://developers.google.com/photos/library/guides/client-libraries) to make this easier. However... if you're looking for an official .NET library then there are zero options.

The *CasCap.Apis.GooglePhotos* library wraps up all the available functionality of the Google Photos REST API in (hopefully!) easy to use methods.

Note: Before you jump in and use this library you should be aware that the Google Photos API only allows the upload/addition of images/videos - no edits or deletion are possible and have to be done by manually!

## Quick Start

Install this package into your project using NuGet ([see details here](https://www.nuget.org/packages/CasCap.Apis.GooglePhotos/)).

The primary API usage is to call IServiceCollection.AddGooglePhotos in the Startup class ConfigureServices method.
```csharp
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddGooglePhotos();
    }
}
```

A few required options should be set, these options can be set via an appsettings.json file;

```json5
// appsettings.json
{
    "CasCap": {
        "GooglePhotos": {
            "User": "your.email@mydomain.com",

            // There are 5 security scopes, some work alone and others can be combined with others.
            // Using both Access and Sharing gives the highest level of access. 
            "Scopes": [
                //"ReadOnly",
                //"AppendOnly",
                //"AppCreatedData",
                "Access"
                //"Sharing"
                ],

            // The ClientId and ClientSecret are provided by the Google Console.
            "ClientId": "012345678901-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.apps.googleusercontent.com",
            "ClientSecret": "abcabcabcabcabcabcabcabc",
        }
    }
}
```

Or the required options can be set via hard-coding into the application;
```csharp
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    ...
    
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddGooglePhotos(options =>
        {
            options.User = "your.email@mydomain.com";
            options.Scopes = new[] { Scopes.Access };
            options.ClientId = "012345678901-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.apps.googleusercontent.com";
            options.ClientSecret = "abcabcabcabcabcabcabcabc";
        });
    }
}
```

Note: For a production system using the appsettings.json is the preferred option but the ClientId & ClientSecret should be stored in a secure facility outside of source control i.e. Azure KeyVault or equivalent.

## In More Detail

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

The [Google.Apis.Auth](https://www.nuget.org/packages/Google.Apis.Auth/) library will cache the OAuth 2.0 login information in a local JSON file which it will then read from and renew tokens if necessary on subsequent logins. The JSON file is stored at Environment.SpecialFolder;

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

- Simple Console App
- Moderate Console App w/ Dependency Injection + appsettings.json
- Advanced Console App w/Generic Host
- [googfotos](https://github.com/f2calv/googfotos) my .NET Core Tool 

### Misc

### Dependencies

Key dependencies of this library;

- [Google.Apis.Auth](https://www.nuget.org/packages/Google.Apis.Auth/) handles all authentication.
- [CasCap.Common.Net](https://www.nuget.org/packages/CasCap.Common.Net/) handles all HttpClient requests.
[see details here](https://www.nuget.org/packages/CasCap.Apis.GooglePhotos/)

### Resources

- https://developers.google.com/photos

### To Do List

todo: create interface that works with imgur also?
https://imgurapi.readthedocs.io/en/latest/