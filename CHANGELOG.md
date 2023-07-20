# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.3]

### Added
- Rotation statistic recording
- Option to flatten multipart statistics (e.g. XYZ) into a single line during CSV export

## [1.1.2] - 11/07/23

### Added
- Added time values to statistic window and CSV exporting

### Fixed
- PlaybackManager kept wiping incorrect component types when assigning playback data

## [1.1.1] - 10/07/23

### Fixed
- Assembly Definitions now look for names instead of GUIDs to prevent OdinSerializer issues

## [1.1.0] - 10/07/23

### Added
- Proper Ultrleap Hand recording through a post-process provider
  - Example scene included

### Changed
- Converted project into a Unity package format for easier consumption and usage
- Improved statistics window graphs
- Changed statistics to use generic types for better usage
- PlayRecorder menu has been moved to Ultraleap -> PlayRecorder

### Fixed
- Minor bug with recording name checks
- Incorrect conversions of RecordItem types
- TransformRecordComponent transform spaces are correctly accounted for
- Correct data path for saving recordings on Android

## [1.0.0 and older]

Refer to the [release notes page](https://github.com/ultraleap/PlayRecorder/releases) for older releases.