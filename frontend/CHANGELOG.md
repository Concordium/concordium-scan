# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

## [1.7.12] - 2025-04-10

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
