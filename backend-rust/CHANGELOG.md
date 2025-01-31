# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Added

- `ccdscan-indexer`: Provide option `--node-tcp-keepalive <duration>` (env `CCDSCAN_INDEXER_CONFIG_NODE_TCP_KEEPALIVE`) to enable TCP keepalive messages for connections to Concordium Nodes.
  Takes the duration in seconds to remain idle before sending TCP keepalive probes.

### Changed

- `ccdscan-api`: Make the `Query::transaction_metrics` use fixed buckets making it consistent with behavior of the old .NET backend.

### Fix

- `ccdscan-api`: Fix unit conversion for `avg_finalization_time` in `Query::block_metrics`.
- `ccdscan-api`: Issue for `Query::transaction_metrics` producing an internal error when query period is beyond the genesis block.

## [0.1.19] - 2025-01-30

Database schema version: 1

From before a CHANGELOG was tracked.
