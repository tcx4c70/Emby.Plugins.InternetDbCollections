# Emby.Plugins.InternetDbCollections

This is an Emby plugin that integrates metadata collections from popular Internet Movie Databases into your Emby media library.

## Overview

The plugin collects metadata information from internet databases (such as IMDb) and automatically updates your Emby library with this data.

## Status

This plugin is **under active development**. There may be breaking changes in the API and functionality as new features and internet databases are added.

## Features
- Supported datasources:
  - [IMDb Top 250 movies](https://www.imdb.com/chart/top/)
  - [IMDb Top 250 TV shows](https://www.imdb.com/chart/toptv/)
  - [Top 100 Greatest Movies of All Time (The Ultimate List)](https://www.imdb.com/list/ls055592025/)
  - General IMDb charts & lists
  - MDB List
- Supported actions:
  - Add tags to media items
  - Create collections and add items to collections

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
  - Trakt
  - ...
- Improvements when scraping datasource:
  - Retry
  - Proxy support
- Logging improvements:
  - Move some logs to debug level

## Contributing

Contributions and suggestions are welcome! Please note the project is in early stages and the API may change.

## License

This plugin is released under the [MIT](LICENSE) License.
