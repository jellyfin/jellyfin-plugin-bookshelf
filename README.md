<h1 align="center">Jellyfin Bookshelf Plugin</h1>
<h3 align="center">Part of the <a href="https://jellyfin.org/">Jellyfin Project</a></h3>

## About
The Jellyfin Bookshelf plugin enables the collection of eBooks & AudioBooks, with the latter being able to be played through Jellyfin.
This plugin uses Google Books as a Metadata provider.

Supported eBook file types:
- epub
- mobi
- pdf
- cbz
- cbr

## Build & Installation Process
1. Clone this repository
2. Ensure you have .NET Core SDK setup and installed
3. Build the plugin with following command:
```
dotnet publish --configuration Release --output bin
```
4. Place the resulting `Jellyfin.Plugin.Bookshelf.dll` file in a folder called `plugins/` inside your Jellyfin installation / data directory.

### Screenshot
<img src=screenshot.png>
