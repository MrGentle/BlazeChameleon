# BlazeChameleon
A cross-platform CLI which runs an API/Interface for SteamWorks and SteamWeb. 
Mainly used for Lethal League Blaze but you should be able to use it for any steam game.

Feel free to contribute.

## Building
1. Clone the repository.
2. Add a static class named Config. Example:
```csharp
public static class Config {
    public const uint APP_ID = 553310; 
    public static string[] STEAM_WEB_API_KEYS = {"Key1", "Key2", ... };
}
```
3. Build for your target.


## Running
Steam needs to be running and logged into to be able to interface with steamworks.
> BlazeChameleon --listen --port=8080

You can also supply the ``--secret`` argument to secure your routes with a password.

Use ``BlazeChameleon --help`` for more info
