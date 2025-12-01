# Changelog

All notable changes to this project will be documented in this file.

## Unreleased 

### Added

- Added `RegisterData` transaction details display with CBOR decoded data.

- Added a Pagination `Load More` button to load more plt activities on the plt specific page.

### Fixed

- Fixed mis-alignment of plt token profile layout on long token names.

- Fixed bug when 7-day analytics charts on plt specific page do not load data properly on the first load.

- Fixed the percentage for all total staked CCD in the validator details sidebar.

- Fixed the incorrect display of transaction commission in the validator details sidebar. 

- Fixed the bug while hovering over the plt chart tooltip, the other charts' labels disappear.

## [1.7.27] - 2025-10-30

### Added

- Enabled plt search functionality in the ui to show the search results for plt tokens.

### Fixed

- Fixed totals supply discrepancy between actual data and ccdscan data on plt overview page.

- Fixed plt dashboard now displays total `10` plt sorted by current supply.

## [1.7.26] - 2025-09-16

### Fixed

- Fixed overview page plt total supply chart to display up to 3 decimal places.

## [1.7.25] - 2025-09-10

### Added

- Added token holder distribution chart to specific plt dashboards.

### Fixed

- Fixed per plt page navigation to open in the same tab.
- Fixed issue with footer links not redirecting to proper telegram channel.
- fix validator `APY` section  `APY` figures were all overlapping resulting in them becoming unreadable.
- PLT overview page supply chart tooltip convert token symbol to token name.
- Change the `Transaction` to `Activities` on the overview page of PLT and the per PLT page.
- Fixed plt transfer memo was not visible.

## [1.7.24] - 2025-08-29

### Fixed

- fix build failure due to the bigint literal in es2019

## [1.7.23] - 2025-08-26

### Fixed

- Fixed transaction event details for `TokenUpdate` not displaying decimal places properly.
- Fixed tooltip label formatting in the doughnut chart to only show the percentage (two decimal places).

### Added

- Added Protocol token holder distribution chart to the plt dashboard.

## [1.7.22] - 2025-07-24

### Changed

- Fixed bug to ensure backward compatibility with `TokenAmount` types.

## [1.7.21] - 2025-07-17

### Changed

- Fixed bug plt dashboard not displaying token distribution chart properly.
- Fixed missing available balance and required balance when displaying an insufficient balance `TokenUpdateReject` reason in the transaction list page.
- `usePagedData` composable has been updated to support optional `pageSize` and `maxPageSize` parameters, allowing for more flexible pagination control.

## [1.7.20] - 2025-07-16

### Changed

- Plt dashboard now uses the graphQL query `usePltEventsQuery` and `usePltTokenQuery` to fetch plt token details and events.
- TokenModuleEvent `pause` and `unpause` events are now displayed in the transaction table(UI).

### Added

- Added `usePltTokenQuery` to fetch details of a specific plt token.
- Added `usePltEventsQuery` to fetch events related to plt tokens.

### Removed

- Remove the temporary maintenance banner.

### Changed

- Move remaining queries to use the new `rust-backend` API.
  - `useSearchQuery`
  - `usePassiveDelegationPoolRewardMetrics`
  - `useBakerTransactionsQuery`
  - `useBakerRewardsQuery`
  - `useBakerDelegatorsQuery`
  - `useAccountsMetricsQuery`
  - `useAccountRewardMetricsQuery`
  - `useAccountQuery`
  - `useAccountsListQuery`
  - `useAccountsUpdatedSubscription`
  - `useBlockSubscription`

- Move account statement export to use `rust-backend` API.
- Update ccd scan copyright to 2025

## [1.7.19] - 2025-07-03

### Removed

- Remove overview section from stablecoin dashboard.

## [1.7.18] - 2025-07-03

### Changed

- Stable coin dashboard now displays plt list and plt supply analytics.

## [1.7.17] - 2025-06-25

### Fixed

- Fix the bug where plt transactions were not displayed properly in the transaction list page.

## [1.7.16] - 2025-06-24

### Added

- Plt TokenHolder and TokenGovernance events are now properly displayed in the Transaction table.

## [1.7.15] - 2025-06-29

### Added

- Implemented stable coin dashboard with the following sections: Overview, Supply, Holders
- Implemented stable coin issuer specific dashboard with the following sections: Overview, Holder table, Holder distribution
- Added environment config `enablePltFeatures` to control stable coin menu display.
- Plt transactions are now displayed in the transaction list page.

## [1.7.14] - 2025-04-30

### Changed

- Generate the graphQL types from the schema of the new Rust backend.
- The `NodeLink` component requires a `node-id` and `node-name` as input now so that the component can be used with the `NodeStatus` type as well as the `PeerReference` type.
- Use the correct `PassiveDelegationSummary` type instead of the `DelegationSummary` type in the `PassiveDelegation` component.
- Handle the case that the types `selfSuspended`, `inactiveSuspended`, or `primedForSuspension` can be `undefined` in `BakerSuspension` component.
- Remove the `CisUpdateOperatorEvent` component as only token events are displayed and the `CisUpdateOperatorEvent` is not associated to any token id.
- Remove the parsed `cis2Event` logs as the events are already displayed in the same component.
- Rename `TokenEvent` to `Cis2Event`, `ContractsEdge` to `ContractEdge`, and `BakerTransactionRelation` to `InterimTransaction` to align with the new schema.
- The `lastCumulativeAccountsCreated`, `lastCumulativeTransactionCount`, and `sumRewardAmount` are optional types now and set to 0 if not presen in reward metrics.
- Extend the type `updateTransactionTypes` with the missing cases `VALIDATOR_SCORE_PARAMETERS_UPDATE`, `GAS_REWARDS_CPV_2_UPDATE`, `MINT_DISTRIBUTION_CPV_1_UPDATE`, `UPDATE_LEVEL_1_KEYS`, and `UPDATE_LEVEL_2_KEYS`.

## [1.7.13] - 2025-04-14

### Fixed

- Support `node_statuses` id format of fungy backend when obtaining legacy backend id format in `NodeLink` component

### Fixed

- `useRewardMetrics` to be using `rust-backend` endpoint

## [1.7.11] - 2025-04-07

### Fixed

- `useRewardMetrics` to be using legacy endpoint

## [1.7.10] - 2025-04-02

### Fixed

- Fix `Accounts` sorting and page information.

### Changed

- Change query `useBakerPoolRewardMetrics` to use the new `rust-backend` API.

### Added

- Support pagination for list of all suspended validators and the list of all primed for suspension validators in `Suspended Validators` drawer.

## [1.7.9] - 2025-03-27

### Changed

- Change queries `useTransactionReleaseSchedule` and `useTransactionQuery` to use the new `rust-backend` API.
- Change query `usePassiveDelegationQuery` to use the new `rust-backend` API.
- Change queries `useVersionsQuery`, `useTopDelegatorsQuery` and `usePaydayStatusQuery` to use the new `rust-backend` API.

## [1.7.8] - 2025-03-21

### Changed

- Change queries `useBakerQuery` and `useBakerListQuery` to use the new `rust-backend` API.

### Added

- Added `Suspended Validators` drawer which displays a list of all suspended validators and a list of all primed for suspension validators.
- Added the `status` (`active`, `suspended` or `primed`) of a baker in the baker details page.
- Included the `status` (`active`, `suspended` or `primed`) for each baker in the baker's list.
- Implemented tooltips for `statuses`, providing additional explanations, such as the reason for suspension (`inactivity` or `self-suspend transaction sent`).

## [1.7.7] - 2025-03-21

### Fixed

- Allowed queries `useRewardMetricsQuery` and `useRewardMetricsForBakerQuery` to use a separate API.

## [1.7.6] - 2025-03-14

### Fixed

- Allowed queries `useBakerMetricsQuery` to use a separate API.

## [1.7.5] - 2025-03-03

### Fixed

- Allowed queries `useNodeDetailQuery` and `useNodeQuery` to use a separate API.

## [1.7.4] - 2025-02-13

### Added

- Add `blockHeight` at block details page.

### Changed

- Always query block by block hash and never by ID.

### Fixed

- Scrolling bar issue after having opened drawer
- Remove locked CCD number

## [1.7.3] - 2025-02-12

### Fixed

- Fix transaction links from transaction list page. These failed due to using a transaction ID which is incompatible with the new API, instead only the transaction hash can be used now.

## [1.7.2] - 2025-02-11

### Changed

- Move query `useTransactionMetricsQuery` and `useTransactionsListQuery` to new API.

## [1.7.1] - 2025-02-07

### Changed

- Always display the `hexDecimal` representation of smart contract logs and messages.

## [1.7.0] - 2025-02-03

### Added

- Add "Last" button for pagination based on cursors.

### Changed

- Move query `useBlockSpecialEventsQuery` to new API.

### Fixed

- Rename `ContractUpgraded.from` and `ContractUpgraded.to` in query `useTransactionQuery` and `useContractQuery` to avoid error of fields which cannot be merged, as these were clashing with `Transferred.from` and `Transferred.to`.

## [1.6.2] - 2025-01-19

### Changed

- Allowed queries `useBlockListQuery`, `useBlockQuery`, `useChartBlockMetrics`, `useContractQuery`, `useContractsListQuery`, `useTokenQuery`, `useTokenListQuery` and `useModuleQuery` to use a separate API.

### Fixed

- Remove unused fields in query for cis2 token events.
- Remove unused sub-query for account transactions in the accounts page.
- Remove unused fields in query for block metrics.

## [1.6.0] - 2024-11-05

### Added

- Temporary maintenance banner.

### Changed

- Migrate project to Nuxt `3.13`.
- Update NodeJS runtime version to `18.12.1`.
- Frontend image is now independent on the network being used, and can be configured at runtime.

### Removed

- Display of pending stake changes for validators and delegators, as this information is no longer relevant starting from Concordium Protocol Version 7.

## [1.5.41] - 2024-03-25

From before a CHANGELOG was tracked.
