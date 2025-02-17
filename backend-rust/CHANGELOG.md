# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Fixed

- Remove locked CCD metrics

## [0.1.25] - 2025-02-14

Database schema version: 5

### Fixed

- Add database migration fixing:
  - Invalid bakers caused by `DelegationEvent::RemoveBaker` event not being handled by the indexer until now.
  - Invalid delegator state, caused by validator/baker getting removed or changing status to 'ClosedForAll' without moving delegators to the passive pool.
- Fixed indexer missing handling of moving delegators as pool got removed or closed.
- Fixed indexer missing handling of event of baker switching directly to delegation.

## [0.1.25] - 2025-02-14

Database schema version: 4

### Added

- Database migration to add the lottery power of each baker pool during the last payday period.
- Add Query `Query::Baker::state::pool::lotteryPower` which returns the `lotteryPower` of the baker pool during the last payday period.

### Changed

- Query the `get_module_source` on the `LastFinal` block to improve performance.

## [0.1.24] - 2025-02-12

### Fixed

- Fix metrics where interval is being shown in greater units than strictly seconds

## [0.1.23] - 2025-02-11

Database schema version: 3

### Added

- Add `payday_commission_rates` to the `Baker::state` query. These rates contain the `transaction`, `baking`, and `finalization` commissions payed out to a baker pool at payday.

### Fixed

- Fix API issue where `Query::block_metrics` computed summary of metrics period using the wrong starting point, causing metrics to be off some of the time.
- Fix API issue where `Query::block_metrics` computed summary of metrics period using blocks which had no finalization time set.
- Fix now on application side in `Query::block_metrics` to ensure data calculated on across different queries are the same.
- Query `Query::transactions` now outputs items in the order of newest->oldest, instead of oldest->newest.

## [0.1.22] - 2025-02-11

### Added

- Add `--node-request-rate-limit <limit-per-second>` and `--node-request-concurrency-limit <limit>` options to `ccdscan-indexer` allowing the operator to limit the load on the node.

### Changed

- Remove `LinkedContractsCollectionSegment::PageInfo`, `ModuleReferenceRejectEventsCollectionSegment::PageInfo`, `ModuleReferenceContractLinkEventsCollectionSegment::PageInfo`, `TokensCollectionSegment::PageInfo` and `ContractRejectEventsCollectionSegment::PageInfo` from API as these are never used by the frontend.

### Fixed

- Revert the API breaking change of renaming `Versions::backend_versions` to `Versions::backend_version`.
- Fix order of module reference reject events to DESC.
- Fix order of module linked contracts events to DESC.
- Fix order of module linked events to DESC.
- Fix issue in API `Query::blocks`, where providing `before` or `after` did not consider the blocks to be descending block height order.
- Fix API `Contract::tokens` and `Contract::contract_reject_events` only providing a single item for its first page.
- Fix issue in API `Query::tokens`, where page information `has_previous_page` and `has_next_page` got switched around.

## [0.1.21] - 2025-02-10

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

- Fix button to display the schema decoding error at front-end by returning the error as an object.
- Fix typo in `versions` endpoint.
- Fix unit conversion for `avg_finalization_time` in `Query::block_metrics`.
- Issue for `Query::transaction_metrics` producing an internal error when query period is beyond the genesis block.
- Contract rejected event skips in the correct way.
- Fix issue where `ContractUpdated::message` attempted to parse empty messages, resulting in parsing error messages instead of `null`.
- Issue making `avgFinalizationTime` field of `Query::block_metrics` always return `null`.
- Next and previous page on contracts.
- Issue making `Query::block_metrics` included a bucket for a period in the future.
- Contract events order fixed

## [0.1.19] - 2025-01-30

Database schema version: 1

From before a CHANGELOG was tracked.
