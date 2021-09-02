<h1 align="center">Jellyfin Bookshelf Plugin</h1>
<h3 align="center">Part of the <a href="https://jellyfin.org/">Jellyfin Project</a></h3>

<p align="center">

<img alt="Logo Banner" src="https://raw.githubusercontent.com/jellyfin/jellyfin-ux/master/branding/SVG/banner-logo-solid.svg?sanitize=true"/>
<br/>
<br/>
<a href="https://github.com/jellyfin/jellyfin-plugin-bookshelf/actions?query=workflow%3A%22Test+Build+Plugin%22">
<img alt="GitHub Workflow Status" src="https://img.shields.io/github/workflow/status/jellyfin/jellyfin-plugin-bookshelf/Test%20Build%20Plugin.svg">
</a>
<a href="https://github.com/jellyfin/jellyfin-plugin-bookshelf">
<img alt="MIT License" src="https://img.shields.io/github/license/jellyfin/jellyfin-plugin-bookshelf.svg"/>
</a>
<a href="https://github.com/jellyfin/jellyfin-plugin-bookshelf/releases">
<img alt="Current Release" src="https://img.shields.io/github/release/jellyfin/jellyfin-plugin-bookshelf.svg"/>
</a>
</p>

## About

The Jellyfin Bookshelf plugin enables the collection of eBooks & AudioBooks, with the latter being able to be played through Jellyfin. This plugin uses Google Books as a Metadata provider.

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
