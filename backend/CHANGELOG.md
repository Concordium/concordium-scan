# Changelog

All notable changes to this project will be documented in this file.

## Unreleased

## [2.0.17] - 2025-07-24

### Added

- Updated Rust SDK submodule in order to fix bug with CBOR decoding empty string


## [2.0.16] - 2025-07-24

### Added

- Introduced batches in migrations m0037 and logging.
- (Bugfix) Proper handling of serialization and deserialization of `InitializationParameters` in `CreatePlt`, `CreatePltUpdate`, `TokenCreationDetails` events.

## [2.0.15] - 2025-07-16

### Changed

- Aligned with Rust SDK version `7.0.0-alpha.3`.

### Added 

- Added plt specific tables `plt_events`, `plt_account`, `plt_tokens` 
- Added query `PltEvents` to fetch events related to plt tokens. 
- Added query `PltToken` to fetch details of a specific plt token.

## [2.0.14] - 2025-07-10

### Changed

- Staked amounts fix: now updated through querying the node
- Fix P3->P4 protocol update migration to allow for bakers pending removal to be absent from the bakers table.

## [2.0.13] - 2025-07-03

### Changed

Database schema version: 38

- Baker APY query db migration to change the function to prevent overflow FLOAT8

## [2.0.12] - 2025-07-03

### Changed

- New account_transaction_type enum values added `TokenUpdate`, Removed `TokenHolder` and `TokenGovernance`
- TokenHolder and TokenGovernance events are now merged into `TokenUpdate` event.
- The ccdscan-api now connects to the grpc node to fetch token list and details (This will be removed later).

## [2.0.11] - 2025-07-01

### Fixed

- Fix for db connections issue where new connections are created during failed block processing, and old connections are not closed and cleaned up effectively

## [2.0.10] - 2025-06-24

### Added

- Plt TokenHolder and TokenGovernance Events are now indexed and available in the API.

## [2.0.9-hotfix.1] - 2025-07-18

- create hotfix for backwards compatability balance statistics api to support calls for: field=TotalAmountReleased and field=TotalAmount which is used by clients

## [2.0.9] - 2025-06-23

- Added configurable environment variable in the code for Account Statements Export for the max allowed days: 'CCDSCAN_API_EXPORT_STATEMENTS_MAX_DAYS' which defaults currently to 32 for all environments.

## [2.0.8] - 2025-06-20

- Fix errors in migration 0024 when running against PostgreSQL `17.5`.
- Fix for delegators paging - previously the sub queries for start cursor and end cursor did not have the IS NOT NULL check, so technically the start and end cursor could be beyond the list for filtered delegators

## [2.0.7] - 2025-06-16

### Fixed

- Added fix for delegators summary query - Delegator summary is now populated by values where the delegated restake earnings is NOT NULL

## [2.0.6] - 2025-06-06

Database schema version: 36

### Fixed

- Added slot_time to account statements table and covering index for searching account statements effectively

## [2.0.5] - 2025-05-30

### Fixed

- fix docker file to use mock data for plt dashboard

## [2.0.4] - 2025-05-29

Database schema version: 35

### Added

- Plt specific transaction events are now indexed and available in the API.

## [2.0.3] - 2025-05-21

### Fixed

- Fix query performance issue when getting account rewards for accounts with large number of account statements to better use the index.

## [2.0.2] - 2025-05-21

Database schema version: 34

### Fixed

- Fix performance issue when getting account rewards for accounts with large number of account statements as migration 34.
- Fix issue in `/rest/export/statement` causing error `Invalid integer: out of range integral type conversion attempted` when exporting account statements for period where the account had negative statement entry (transfer or transaction fee).

## [2.0.1] - 2025-05-19

### Fixed

- Fix performance issue in `Query::bakerMetrics`.

## [2.0.0] - 2025-05-13

Database schema version: 33

### Removed

- Remove pool related options from `ccdscan-indexer`, that is `--min-connections` (env `CCDSCAN_INDEXER_DATABASE_MIN_CONNECTIONS`) and `--max-connections` (env `CCDSCAN_INDEXER_DATABASE_MAX_CONNECTIONS`).

### Fixed

- Fix issue in `Account::tokens` showing the last page first and mismatching page information.
- Fix edge case for `BakerPool::delegators` and `PassiveDelegation::delegators` when the outer sorting (delegated stake) are equal for the page bounds, the inner sorting (account index) got ignored.
- Fix missing node `ema` and `emsd` values reported by the node collector backend.
- Fix performance issue in `Query::transactionMetrics`.
- Fix the rewards query to search correct from and to values - originally was incorrect was searching from the to value and to the from value.
- Fix the account metrics query to provide the right max index when searching for accounts created.

### Added

- Add partial contract index search to the query `SearchResult::contracts`.
- Add database migration 33 to add a column `index_text` to the `contracts` table. An index is added over that column to support partial contract index searches.
- Introduce lock preventing multiple instances of `ccdscan-indexer` from indexing at the same time. The process will wait for the lock otherwise exit with an error. The wait duration defaults to 5 seconds and can be configured using `--database-indexer-lock-timeout` (env `CCDSCAN_INDEXER_CONFIG_DATABASE_INDEXER_LOCK_TIMEOUT`).
- Balance statistics api: added the deprecated `TotalAmounUnlocked` parameter for backwards compatability with comments that it will be removed in a future release. `TotalAmountUnlocked` is now the preferred parameter for this api.
- Add `/playground` endpoint for `ccdscan-api` for access to GraphQL Playground IDE.
- Add monitoring metric `api_rest_request_duration_seconds` tracking response status code and response duration time for requests for the public REST API.

### Changed

- Passing `--schema-out <file>` option to the `ccdscan-api` binary no longer requires any other options such as the database URL to be set.
- Hide internal errors from the `ccdscan-api` GraphQL API response and produced an ERROR level log message instead.
- The `ccdscan-indexer` binary no longer uses a database pool, reducing the overhead and number of connections to the database.
- A dummy server for plt support and which new queries are now exposed at the backend (Will be removed later).

## [0.1.51] - 2025-04-30

### Changed

- Rename `AccountMetrics` to `AccountsMetrics` for backwards compatibility with the old dotnet backend.
- `Token::token_events` returns the `Cis2Token` event directly now instead of wrapping it into an option.

## [0.1.50] - 2025-04-28

Modifying search key on the following tables to all be using `text_pattern_ops` because it is faster when using prefix searches only

- `accounts`
- `blocks`
- `tokens`

## [0.1.49] - 2025-04-28

- Bump lock file to reflect new SDK version

## [0.1.48] - 2025-04-28

### Added

- Add `SearchResult::node_statuses`.
- Add `SearchResult::tokens`.
- Add `SearchResult::modules`.

### Fixed

- Optimise `Account::transactions` performance
- Fix potential issue on `accountMetrics` which occurred when a blocks containing account creation occurred on the exact same time as the borders of the buckets slots
- Fix query performance issues on `accountMetrics` when dataset becomes too large
- Fix account statements performance issues where conditions was being used without indexes
- Fix account statements where when querying an address using the complete address and not the canonical address
- Fix account statements using DESC ordering per default instead of ASC
- Fix account rewards using DESC ordering per default instead of ASC
- Fix account transactions using DESC ordering per default instead of ASC
- Fix account transaction altering index to be account index first
- Fix account transactions queries to be using WHERE instead of join
- Fix baker transactions pagination
- Fix baker delegation pagination to be sorted by stake as primary instead of using account index.

## [0.1.47]

### Fixed

- Fix issue where `ccdscan-api` GraphQL resolvers fail to load the config from the context.

## [0.1.46] - 2025-04-10

### Added

- Add `SearchResult::bakers` and `SearchResult::contracts`.
- Add query `Query::poolRewardMetricsForPassiveDelegation`.
- Add REST API `/rest/export/statement` for exporting account statements as CSV.
- Add REST API `rest/balance-statistics/latest` for querying total amount in the network, used by external parties.
- Add `ccdscan-api` option `--statement-timeout-secs` (env `CCDSCAN_API_DATABASE_STATEMENT_TIMEOUT_SECS`) for configuring a statement timeout the database connections and abort any statement that takes more than the specified amount of time. Defaults to 30 seconds.

### Fixed

- Reordered the primary key on the `reward_metrics` table from (account_index, block_slot_time) to (block_slot_time, account_index) to improve query performance.

## [0.1.45] - 2025-04-03

Database schema version: 27

### Fixed

- Fix pagination issue for `Query::bakers` when page bounds start and end with the same value for the selected sorting, it would include an overlap for the nested sorting (usually the validator ID).
- Fix double counting of rewards in `Query::poolRewardMetricsForBakerPool` when paydays are exactly on the edge of two buckets.

## [0.1.44] - 2025-04-02

Database schema version: 27

### Added

- Add query `Query::poolRewardMetricsForBakerPool`.

### Fixed

- Fixed issue when computing of `ActiveBakerState::delegated_stake_cap` where the order of operations were wrong, resulting in a cap of `0` for some validator pools.
- Add database migration 27 reindexing credential deployments to include the Credential Registration ID in the events and to update the fee cost to 0.
- Fix `Query::bakers` when sorted by block commission rate (both ascending and descending) to include bakers, which does not have a block commission for the current reward period.
- Add database migration 26 updating pool information for the ones missing.
  Since the baker/validator pool concept was introduced as part of Concordium protocol version 4, bakers/validators prior to this protocol version implicitly became pools and until now the indexer did not update the pool information for these.
- Add database migration 25 reindexing the stake for delegators to the passive pool.
  The stakes for these delegators got offset by a bug in migration 13, which did not update the passive delegators properly.
  This was fixed again in migration 22, but the passive delegators with restake_earnings enabled missed their increase in stake due to this.
- Fixed `PassiveDelegation::delegators` sorting and page information.
- Fix accidental breaking change due to typo in `BakerSort` variants `BAKER_APY30_DAYS_DESC` and `DELEGATOR_APY30_DAYS_DESC`.

## [0.1.43] - 2025-03-27

### Fixed

- Fix issue for database migration 23 where it failed when processing a transaction containing a rejected smart contract initialization.

## [0.1.42] - 2025-03-27

Database schema version: 24

### Added

- Add database migration 24 precomputing the APYs for each validator pool over 30 days and 7 days.
- Support for `Query::bakers` sorting options: validator APY and delegators APY.
- Add database migration 23 adding the `input_parameter` to `ContractInitializedEvents`.
- Expose the hex-encoded and schema-decoded input parameter of init functions via `ContractInitialized::message_as_hex` and `ContractInitialized::message` queries.

## [0.1.41] - 2025-03-26

Database schema version: 22

### Added

- `baker_id` to `AccountAddress` in transaction events.

### Fixed

- Fixed bug when setting `--log-level` causing some project logs to be filtered out, and filter dependencies to `info` level. This behavior can be overwritten using `RUST_LOG`.

## [0.1.40] - 2025-03-26

Database schema version: 22

### Fixed

- Rename `ValidatorScoreParametersChainUpdatePayload` to `ValidatorScoreParametersUpdate` for backwards compatibility on dotnet
- Fix the `AccountByAddress::transactionCount` query to return the number of transactions the account has been involved in or
  affected by. Expose the `AccountByAddress::nonce` query to return the account nonce.

## [0.1.39] - 2025-03-25

Database schema version: 22

### Added

- Add support for ChainUpdatePayload events.
- Add support for `TransferredWithSchedule::amountsSchedule`

### Fixed

- Add database migration 22 fixing some passive delegators that had no `delegated_restake_earnings` value set in the database.
- Change `Versions::backend_versions` to `Versions::backend_version`.
- When `effective_time` is zero then it translates into `Block::slot_time`.
- Split up migration 18 into several SQL transactions to avoid timeouts for long running migrations.
- Change CLI option `--log-level` to only apply for logs produced directly from this project, instead of including every dependency.

## [0.1.38] - 2025-03-21

Database schema version: 18

### Added

- Add query `SuspendedValidators::suspendedValidators`.
- Add query `SuspendedValidators::primedForSuspensionValidators`.
- Add database migration 18 adding table tracking baker pool and passive stake for every reward period.
- Add query `PassiveDelegation::apy` and `BakerPool::apy`.

## [0.1.37] - 2025-03-21

Database schema version: 17

### Added

- Add database migration 17 adding a table tracking reward metrics.
- Add database migration 16 adding a table tracking commission rates for passive delegation and adding an index to retrieve passive delegators efficiently from the accounts table.
- Add query `PassiveDelegation::delegators`, `PassiveDelegation::delegatorCount`,
  `PassiveDelegation::commissionRates`, `PassiveDelegation::delegatedStake` and `PassiveDelegation::delegatedStakePercentage`.
- Add `Query::rewardMetrics` and `Query::rewardMetricsForAccount` which returns metrics on the total rewards and those for a given account respectively.

### Fixed

- Total count to the connection holding the events being emitted as part of the transaction query.

## [0.1.36] - 2025-03-17

Database schema version: 15

### Fixed

- Baker metrics to be using the same time interval for buckets and total values.

### Added

- Add database migration 15 adding a table tracking rewards paid to delegators, passive delegators, and baker accounts at past payday blocks and populate the table.
- Add query `PassiveDelegation::poolRewards` which returns the rewards paid to passive delegators at past payday blocks.
- Add query `BakerPool::poolRewards` which returns the rewards paid to delegators and the baker at past payday blocks.
- Support for `Query::bakers` sorting by block commission.

## [0.1.35] - 2025-03-14

Database schema version: 14

### Added

- Add baker metrics endpoints
- Gathering statistics about change in amount of bakers
- Migrate bakers statistics
- Indexing genesis block creates baker metrics

## [0.1.34] - 2025-03-13

Database schema version: 13

### Added

- Support `include_removed` flag for `Query::bakers` query.
- Add database migration 12 adding table tracking the removed bakers and populate the table.
- Indexer now maintains the removed bakers table.

### Fixed

- Fixed bug in indexer where removed delegators still had the restake earnings flag stored as false instead of NULL.
- Fixed bug in indexer where accumulated pool delegator count is updated after delegator is removed (only relevant for blocks prior to Protocol Version 7).
- Fixed bug in indexer where delegators which set their target to a removed pools did not get updated, now they are moved directly to the passive pool instead (only relevant for blocks prior to Protocol Version 7).
- Fixed bug in indexer where CIS-2 transfer events were never recorded when happening before any other token events (like a Mint). This is considered a bug in the token contract, but now the indexer still records these events.

## [0.1.33] - 2025-03-06

Database schema version: 11

### Fixed

- Return `None` as ranking instead of an internal error for non-baker accounts and bakers that just got added until the next payday.
- Fix underflow in `capital_bound` formula by saturating the value to 0.

### Added

- Add `Node::node_status` to retrieve the node status from a single node including additional data required
- Add `Baker::node_status` to retrieve the validator's node information.
- Add more context to errors during indexing, providing better error messages for debugging.
- Add database migration 11 adding columns to store the ranking of bakers.
- Add query `BakerPool::rankingByTotalStake` which returns a ranking of the bakers by their lottery power. The ranking is re-computed for each payday block.

## [0.1.32] - 2025-02-28

### Fixed

- Fix bug preventing the indexer from handling delegators targeting the passive pool.

## [0.1.31] - 2025-02-28

### Fixed

- Only update delegation target when baker/validator is not removed. This is only relevant for blocks in protocol versions prior to P7 as there it was still possible to target removed bakers/validators during the cooldown period.

## [0.1.30] - 2025-02-27

### Fixed

- Relaxing validation constraint on all database rows affecting bakers requiring a single row update prior to protocol version 7.

## [0.1.29] - 2025-02-26

### Fixed

- Relaxing validation constraint on database rows related to prepared bakers events when protocol 7.

## [0.1.28] - 2025-02-26

Database schema version: 10

### Fixed

- Make validation of rows changed to account tokens accept the zero rows modified

### Added

- Add database migration 10 to store the `leverage_bound` and the `capital_bound` values of the chain.
- Add query `BakerPool::delegatedStakeCap` that considers the leverage and capital bounds to report the delegate stake cap for baker pools.

## [0.1.27] - 2025-02-24

### Fixed

- Removal of duplication on affected accounts in transactions.
- Altering constraint in the database on the stacked amount in the pool to be allowing zero value.

## [0.1.26] - 2025-02-20

Database schema version: 8

### Changed

- Use canonical address instead of account address while referring to accounts.

### Added

- Add validation of the affected rows for most mutating queries in the indexer. This allow the indexer to fail faster for these unexpected conditions.
- Add query `Query::bakers` to the API, but without support for filtering removed bakers and sorting by APY and block commissions, this is to be added soon.
- Add database migration 7 adding accumulated pool state to bakers table, for faster filtering based on these values.
- Add query `BakerPool::delegators` which returns the delegators of a baker pool.
- Store canonical address as a column on the account.

### Fixed

- Add database migration 6 fixing invalid baker and delegator stake due to missing handling of restake earnings.
- Indexer now updates stake when restake earnings are enabled for bakers and delegators.
- Remove locked CCD metrics.
- Add database migration 5 fixing:
  - Invalid bakers caused by `DelegationEvent::RemoveBaker` event not being handled by the indexer until now.
  - Invalid delegator state, caused by validator/baker getting removed or changing status to 'ClosedForAll' without moving delegators to the passive pool.
  - Invalid account balance for account statements, where the change in amount got accounted twice.
- Fixed indexer missing handling of moving delegators as pool got removed or closed.
- Fixed indexer missing handling of event of baker switching directly to delegation.
- Fixed indexer account twice for the changed amount in account statements.

## [0.1.25] - 2025-02-14

Database schema version: 4

### Added

- Database migration to add the lottery power of each baker pool during the last payday period.
- Add query `BakerPool::lotteryPower` which returns the `lotteryPower` of the baker pool during the last payday period.
- Implement `SearchResult::transactions` and add relevant index to database

### Changed

- Query the `get_module_source` on the `LastFinal` block to improve performance.

## [0.1.24] - 2025-02-12

### Fixed

- Fix metrics where interval is being shown in greater units than strictly seconds.

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
