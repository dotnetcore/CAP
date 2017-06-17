# Change Log
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).

## [Unreleased]
_Nothing yet..._

## [0.7.0] - 2017-06-08
### Changed
- SqlServer: Set migration history's table name and schema.

### Fixed
- Services not being disposed correctly after a job execution. [#4](https://github.com/mrahhal/MR.AspNetCore.Jobs/issues/4)

## [0.6.0] - 2017-04-22
### Changed
- SqlServer: Move to using EFCore to manage internal migrations and connections to the database.

[Unreleased]: https://github.com/mrahhal/MR.AspNetCore.Jobs/compare/0.7.0...HEAD
[0.7.0]: https://github.com/mrahhal/MR.AspNetCore.Jobs/compare/0.6.0...0.7.0
[0.6.0]: https://github.com/mrahhal/MR.AspNetCore.Jobs/compare/0.5.0...0.6.0
