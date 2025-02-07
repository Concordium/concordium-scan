# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

Database schema version: 2

### Added

- Add `delegator_count`, `delegated_stake`, `total_stake`, and `total_stake_percentage` to baker pool query.
- Add index over accounts that delegate stake to a given baker pool.
- Add `database_schema_version` and `api_supported_database_schema_version` to `versions` endpoint.
- Add database schema version 2 with index over blocks with no `cumulative_finalization_time`, to improve indexing performance.
- Implement `SearchResult::blocks` and add relevant index to database

### Changed

- Make the `Query::transaction_metrics` use fixed buckets making it consistent with behavior of the old .NET backend.
- Change the log level of when starting the preprocessing of a block into DEBUG instead of INFO.
- Query `Token::token_events` and `Query::tokens` now outputs the events in the order of newest->oldest, instead of oldest->newest.

### Fixed

- Fix typo in `versions` endpoint.
- Fix unit conversion for `avg_finalization_time` in `Query::block_metrics`.
- Issue for `Query::transaction_metrics` producing an internal error when query period is beyond the genesis block.
- Contract rejected event skips in the correct way.
- Fix issue where `ContractUpdated::message` attempted to parse empty messages, resulting in parsing error messages instead of `null`.
- Issue making `avgFinalizationTime` field of `Query::block_metrics` always return `null`.
- Next and previous page on contracts.
- Issue making `Query::block_metrics` included a bucket for a period in the future.
## [0.1.19] - 2025-01-30

Database schema version: 1

From before a CHANGELOG was tracked.
