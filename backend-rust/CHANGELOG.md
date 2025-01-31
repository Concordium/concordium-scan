# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Changed

- Make the `Query::transaction_metrics` use fixed buckets making it consistent with behavior of the old .NET backend.

### Fix

- Fix unit conversion for `avg_finalization_time` in `Query::block_metrics`.
- Issue for `Query::transaction_metrics` producing an internal error when query period is beyond the genesis block.

## [0.1.19] - 2025-01-30

Database schema version: 1

From before a CHANGELOG was tracked.
