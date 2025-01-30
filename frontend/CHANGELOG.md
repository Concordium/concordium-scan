# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

- Move query `useBlockSpecialEventsQuery` to new API.
- Rename `ContractUpgraded.from` and `ContractUpgraded.to` in query `useTransactionQuery` to avoid error of fields which cannot be merged, as these were clashing with `Transferred.from` and `Transferred.to`.

## [1.6.2] - 2025-01-19

### Changed

- Allowed queries `useBlockListQuery`, `useBlockQuery`, `useChartBlockMetrics`, `useContractQuery`, `useContractsListQuery`, `useTokenQuery`, `useTokenListQuery` and `useModuleQuery` to use a separate API.

### Fix

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
