# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

Database schema version: 3

### Added

- Add database schema version 2 with index over blocks with no `cumulative_finalization_time`, to improve indexing performance.

### Changed

- Make the `Query::transaction_metrics` use fixed buckets making it consistent with behavior of the old .NET backend.
- Implement SearchResult::bakers and add relevant index to database
- Change the log level of when starting the preprocessing of a block into DEBUG instead of INFO.

### Fix

- Fix unit conversion for `avg_finalization_time` in `Query::block_metrics`.
- Issue for `Query::transaction_metrics` producing an internal error when query period is beyond the genesis block.

## [0.1.19] - 2025-01-30

Database schema version: 1

From before a CHANGELOG was tracked.
