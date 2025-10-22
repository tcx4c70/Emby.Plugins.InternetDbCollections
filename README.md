# Emby.Plugins.InternetDbCollections

This is an Emby plugin that integrates metadata collections from popular Internet Movie Databases into your Emby media library.

## Overview

The plugin collects metadata information from internet databases (such as IMDb) and automatically updates your Emby library with this data.

## Status

This plugin is **under active development**. There may be breaking changes in the API and functionality as new features and internet databases are added.

## Features
- Supported datasources:
  - IMDb charts, e.g.: [IMDb Top 250 movies](https://www.imdb.com/chart/top/), [IMDb Top 250 TV shows](https://www.imdb.com/chart/toptv/)
  - IMDb lists, e.g.: [Top 100 Greatest Movies of All Time (The Ultimate List)](https://www.imdb.com/list/ls055592025/)
  - MDB List, e.g.: [Action Movies (Top Rated From 1980 to Today)](https://mdblist.com/lists/hdlists%2Flatest-hd-action-movies-from-1980-to-today)
  - Trakt List, e.g.: [Walt Disney Animated feature films](https://trakt.tv/lists/1262138), [Studio Ghibli Feature Films](https://trakt.tv/lists/801239)
  - Letterboxd, e.g.: [Official Top 250 Narrative Feature Films](https://letterboxd.com/dave/list/official-top-250-narrative-feature-films/), [Movies everyone should watch at least once during their lifetime](https://letterboxd.com/fcbarcelona/list/movies-everyone-should-watch-at-least-once)
- Supported actions:
  - Add tags to media items
  - Create collections and add items to collections
  - Generate external URL links for collections

## Installation

1. Download the plugin DLL from the release or build it from source.
2. Place the DLL in your Emby's plugins directory (usually `plugins` folder in Emby server).
3. Restart the Emby server.
4. The plugin will register and create scheduled tasks automatically.

## Uninstallation
1. (Only when you don't want to keep the tags and collections created by the plugin) Run "Cleanup Metadata" task to cleanup tags and collections created by the plugin.
2. Remove the plugin DLL from the Emby plugins directory.
3. Restart the Emby server.

## Usage

Once installed, the plugin automatically manages collections. It includes scheduled tasks to fetch data from sources like IMDb.
Access the collections in your Emby library, and use the plugin's configuration to customize behavior.

## Configuration

You can configure the plugin through the Emby web interface under the plugins section. Here you can enable or disable specific data sources, and customize collection behavior.

## TODO

- Other datasource, such as:
  - Douban (Maybe it's hard to support it since we really need to parse the HTML page and it's a little hard to introduce third-party library into an Emby plugin)
  - ...
- Improvements when scraping datasource:
  - Proxy support
- Logging improvements:
  - Move some logs to debug level

## Contributing

Contributions and suggestions are welcome! Please note the project is in early stages and the API may change.

## License

This plugin is released under the [MIT](LICENSE) License.
