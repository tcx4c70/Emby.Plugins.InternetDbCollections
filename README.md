# Emby.Plugins.InternetDbCollections

This is an Emby plugin that integrates metadata collections from popular Internet Movie Databases into your Emby media library.

## Overview

The plugin collects metadata information from internet databases (such as IMDb) and automatically updates your Emby library with this data.

For now, it only supports the IMDb Top 250 movies collection and will apply matching tags to your local media items.

## Status

This plugin is **under active development**. There may be breaking changes in the API and functionality as new features and internet databases are added.

## Features
- Supported datasources:
  - IMDb Top 250 movies collection
- Supported actions:
  - Add tags to media items

## Installation

1. Download the plugin DLL from the release or build it from source.
2. Place the DLL in your Emby's plugins directory (usually `plugins` folder in Emby server).
3. Restart the Emby server.
4. The plugin will register and create scheduled tasks automatically.

## Usage

Once installed, the plugin automatically manages collections. It includes scheduled tasks to fetch data from sources like IMDb.
Access the collections in your Emby library, and use the plugin's configuration to customize behavior.

## Configuration

TODO

## TODO

- Emby configuration page
- Cleanup task to remove tags and remove items from collections
- More datasources and collections:
  - Generic IMDb chart support
  - Generic IMDb list support

## Contributing

Contributions and suggestions are welcome! Please note the project is in early stages and the API may change.

## License

This plugin is released under the [MIT](LICENSE) License.
