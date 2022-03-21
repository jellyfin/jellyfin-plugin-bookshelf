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

The Jellyfin Bookshelf plugin enables the collection of eBooks & AudioBooks, with the latter being able to be played through Jellyfin.

### Supported eBook file types:

- epub
- mobi
- pdf
- cbz
- cbr

### Supported audio book file types:

Please take in mind that his is not a complete list and represents some of the most commonly used formats.

- mp3
- m4a
- m4b
- flac

### Offline Metadata providers:

This plugin supports the following offline Metadata providers. These will check the local files for metadata.

- [Open Packaging Format (OPF)](http://idpf.org/epub/20/spec/OPF_2.0.1_draft.htm)
- Calibre OPF
- [ComicInfo](https://github.com/anansi-project/comicinfo)
- [ComicBookInfo](https://code.google.com/archive/p/comicbookinfo/)

The following **limitations** apply:
- .cbr Comics tagged with ComicRacks ComicInfo format are partially supported. Any metadata within the comic book itself will be ignored while external metadata within a ComicInfo.xml file can be read.
- The _[Advanced Comic Book Format](https://launchpad.net/acbf)_ is not supported.
- The _[CoMet](https://www.denvog.com/comet/comet-specification/)_ format is not supported.

### Online Metadata providers:

These Metadata providers will check online services for metadata.

- Google Books


## Build & Installation Process

1. Clone this repository

2. Ensure you have .NET Core SDK setup and installed

3. Build the plugin with following command:

```
dotnet publish --configuration Release --output bin
```

4. Place the resulting `Jellyfin.Plugin.Bookshelf.dll` file in a folder called `plugins/` inside your Jellyfin installation / data directory.
