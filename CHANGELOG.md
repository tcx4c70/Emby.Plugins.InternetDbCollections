# Changelog

All notable changes to this project are documented in this file. This file was generated from git tags.

## Unreleased

## v0.9.1.0 - 2025-12-17

### Changed
- build(dep): Update MediaBrowser.Server.Core to 4.9.1.90 (febf3a6)
- Update version to 0.9.1.0 (47a8374)

## v0.9.0.0 - 2025-12-01

### Added
- feat(UI): Add description for name (669d667)
- feat(UpdatePluginTask): Convert md to html in the overview of update notification (1387767)

### Changed
- refactor(UI): Refactor message of duplicate collector (14b41fc)
- Update version to 0.9.0.0 (825433f)

### Fixed
- fix: log format (03b229c)
- fix(UpdatePluginTask): Avoid update plugin multiple times (fe1a60d)
- fix(UI): Avoid create duplicate collectors with same type and ID (b308878)
- fix(typo): idex -> index (85468e9)
- fix: Parse index to number (54c7da2)

## v0.8.0.0 - 2025-10-31

### Added
- feat: Add jitter to retry (082dead)
- feat: Add HttpClientPool for efficient HTTP client management (ee1b691)

### Changed
- chore: Replace NotImplementedException with NotSupportedException (adf189a)
- perf(Letterboxd): Reduce memory usage by using HttpClient (1b269a9)
- refactor(Collector): Switch to HttpClientPool (4c4989c)
- refactor: Remove unused code (7459a07)
- Update version to 0.8.0.0 (1962832)

### Fixed
- fix: Deplicate items when querying from Emby (773b166)

## v0.7.0.0 - 2025-10-25

### Added
- feat: Support retry during scraping (a867705)

### Changed
- refactor: Remove duplicate private members (77c8fcb)
- perf: Make all collectors run in parallel (8d581bc)
- chore: Use different log names for different collectors (808ee42)
- chore(ScheduledTasks): Add more logs (b12ac9b)
- perf(MetadataManager): Update items in batch to improve performance (1010d02)
- perf(LetterboxdCollector): Improve performance of scraping IMDb IDs (e801fc9)
- Update version to 0.7.0.0 (ef0e684)

### Fixed
- fix: Race condition during update/cleanup metadata (5aaadc2)
- fix: UpdateMetadataTask doesn't update metadata after CleanupMetadataTask (2b347f7)

## v0.6.0.0 - 2025-10-09

### Added
- feat(Collector): Support Letterboxd (88f4be7)

### Changed
- build: Upgrade to Emby 4.9 & .Net 8 (4c1e495)
- chore: Enable nullable check (3884c32)
- chore: style (acf7944)
- build(dep): Update MediaBrowser.Server.Core to 4.9.1.80 (8e273dc)
- perf(Collector/Letterboxd): Get IMDb IDs in parallel to speed up (d4bc687)
- Update README (6b31216)
- Update version to 0.6.0.0 (981066d)

### Fixed
- fix: typo (5e7ed72)
- fix(MetadataManager): Avoid overwrite a locked field (143c889)
- fix(Collector/TraktList): Add User-Agent to avoid 403 forbidden (4debfac)
- fix(Collector/Letterboxd): Fix several bugs during paring (a49858e)
- fix(MetadataManager): Expression tree is too large (dac3ffb)
- fix: External URL for Letterboxd (a460576)

## v0.5.0.0 - 2025-09-30

### Added
- feat: Support cron schedule (dc71670)

### Changed
- refactor(MdbList): Use coherent typed model mapping (b636fd1)
- refactor(Trakt): Use coherent typed model mapping (4ab8f19)
- refactor: Remove spaces (681d8d2)
- chore: Add .editorconfig (c5cd8b4)
- refactor: style & format (b6ac633)
- chore: Drop System.Linq.Async dependency (229692b)
- refactor: Remove EnumerableExtension (ff9f5a4)
- refactor: Move BaseItemComparer&ProgressWithBound to Utils (f220276)
- refactor: Move some types to Models/Collection (a7cad59)
- build: Use ILRepack to merge dependencies (48b09e5)
- chore: Add NOTICE for bundled dependencies (47fe36c)
- build(dep): Update ILRepack.Lib.MSBuild.Task to 2.0.44 (d87a878)
- Update version to 0.5.0.0 (a55aaad)

### Fixed
- fix: Wrong external URL if using id instead of name for mdblist (e29f2d1)
- fix(typo): Empy -> Emby (144b60b)
- fix(configPage): Unsaved changes will be reverted after adding a new collector (17c8fda)
- fix: Button name should be 'Save' for editing collectors (fded7c2)

## v0.4.0.0 - 2025-08-09

### Added
- feat: Support Emby external ID (66fe782)

### Changed
- refactor: Remove ICollectionItem (822da61)
- refactor: Merge ProviderNames and Id (f27c4ab)
- chore: Update README (a5468e7)
- Update version to 0.4.0.0 (1733684)

## v0.3.0.0 - 2025-08-08

### Added
- feat: Support MDB List (2f3d6e8)
- feat: Support MDB List in frontend (441ac75)
- feat: Support Trakt list (b84c8b9)

### Changed
- refactor: Switch to use Plugin.Instance.Logger for updateting task (26d2cf9)
- refactor: Split ICollector (bf17323)
- refactor: Rename tasks (94af211)
- refactor(MetadataManager): Avoid hardcode item types (b483c3f)
- refactor: Prevent saving conf everytime add a collector (d74c0dd)
- chore: Update README (50ef50d)
- refactor: Move CollectorType to a file (87d35cc)
- refactor: Move CollectorConfiguration to a file (c507abb)
- Update version to 0.3.0.0 (4f1dd36)

### Fixed
- fix: Return immediately if cancellationToken is cancelled (35ab63a)

## v0.2.0.0 - 2025-08-02

### Added
- feat: Show update logs in activity (bdc9020)

### Changed
- Update version to 0.2.0.0 (5c9fd30)

## v0.1.0.0 - 2025-08-02

### Added
- feat: Support custom collector names (98b3ebb)
- feat: Add a thumb (dd4988f)
- feat: Add auto update task (029bf21)

### Changed
- chore: Update exception logging (13ac894)
- Update version to 0.1.0.0 (30ece14)

## v0.0.6-alpha - 2025-08-02

### Added
- feat(configPage): Support collector editing (27bef28)

### Changed
- chore: Update README (e2a5ec8)
- refactor(configPage.js): Cache config in front end (3bcec0e)
- chore: Update README (782f895)
- Update version to v0.0.6 (6764019)

## v0.0.5-alpha - 2025-07-27

### Added
- feat: Change the default trigger to 30 days (9aa2e89)
- feat(CollectInternetDbTask): Log exception stacktrace (38b3e11)
- feat: Support configuration page (d0e8a12)
- feat: Add CleanupInternetDbTask (6ea70be)

### Changed
- chore: Update README (8ece21e)
- refactor: Add CollectorBuilder (be2d694)
- chore: Update README (ab080ef)
- Update version to v0.0.5 (40e2ad3)

### Fixed
- fix(ImdbChartCollector): Fix items parsing (63fca4e)
- fix: Fix divide by zero error (77ee5ef)
- fix(ImdbChartCollector): Fix IMDb ID parsing (a0dd4dc)

## v0.0.4-alpha - 2025-07-27

### Added
- feat: Add Name to ICollector (de775ab)
- feat(ImdbCollector): Support collection (184e64c)

### Changed
- refactor: Add BuildCollectors (0ad385b)
- chore: Fix style (34c78d2)
- perf(ImdbCollector): Only update items necessarily (9a482ca)
- chore(ImdbCollector): Update log (b9bc4c1)
- refactor(ImdbCollector): Support to disable tag or collection update (4d8f2fd)
- Update version to 0.0.4 (d6c74fa)

## v0.0.3-alpha - 2025-07-26

### Added
- feat: Support Top 100 Greatest Movies of All Time (The Ultimate List) (0a6d3dc)

### Changed
- refactor: Add ImdbCollector (52d5b70)
- Update version to 0.0.3 (c1d6e83)

## v0.0.2-alpha - 2025-07-26

### Added
- feat: Support IMDb Top 250 TV shows (5458500)

### Changed
- refactor: Add a generic IMDb chart collector (ddba3ab)
- chore: Update .gitignore (5d9bdac)
- chore: Update logs for ImdbChartCollector (68fda67)

## v0.0.1-alpha - 2025-07-26

### Added
- feat: init commit (a179e1e)
- feat: Add IMDb Top 250 collector (2d88dd2)
- feat: Add an empty configuration (475013b)

### Changed
- chore: Add .gitignore (3f9f224)
- refactor: Use one logger for the plugin (b1fb55d)
- chore: add README.md (c5cd35a)

### Fixed
- fix: Miss DayOfWeek in the default trigger (c2d2d79)

