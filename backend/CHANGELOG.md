## Unreleased changes

## 1.10.1

- Added
  - Expand `ChainParametersV3` params takin.

## 1.10.0

- Added
  - Minimal support for Concordium Protocol Version 8.
  - Introduce `ChainParametersV3` in the GraphQL API.

## 1.9.2

- Bugfix
  - Run P6-P7 migration logic for pending changes of delegators in every P7 blocks instead of only at paydays.

## 1.9.1

- Bugfix
    - Change APY computation to be based on the expected payday length instead of the last measure payday length. ([#217](https://github.com/Concordium/concordium-scan/pull/217))
    - Fix crashing issue for when a block contains transactions for both deploying a smart contract module and initializing a smart contract from this module. ([#213](https://github.com/Concordium/concordium-scan/pull/213))

## 1.9.0
- Support Concordium Protocol Version 7.
  - Transition between Delegation and Validating immediately.
  - Stake changes are immediate.
- Fix bug in validation which was branching on the node software version instead of the protocol version.

## 1.8.19
- Bugfix
    - Fix performance of account statement export, by adding an index on table `graphql_account_statement_entries`, and streaming the data.

## 1.8.18
- Bugfix
    - Fix performance view `graphql_account_rewards` by adding an index on table `graphql_account_statement_entries`.

## 1.8.17
- Bugfix
    - Fix jobs `_06_AddTokenAddress`, `_07_ContractSnapshotInitialization` and `_05_CisEventReinitialization` such that they are able to run from an empty database and hence on a initial environment.

## 1.8.16
- Updated
    - Added Contract Snapshot entity to improve frontend query performance.

## 1.8.13
- Updated
    - StatementExport now defaults time period to "31 days ago" to "now", and requires period to not exceed 32 days.

## 1.8.12
- Bugfix
    - Fixed configurations.

## 1.8.11
- Added
    - Extended search functionality to include tokens.

## 1.8.10
- Updated
    - Changed calculations of finalization time to use last finalized block hash from block info instead of using finalization summaries. Changes are needed since finalization summaries isn't present in protocol 6. Using latest finalization block hash in block info is a robust calculation in both old (before protocol 6) and new consensus protocol.

## 1.8.9
- Updated
    - Handle FFI errors through the use of error codes rather than string comparison.
    - Token view on account details page now sort tokens by contract- index, subindex and token id.

## 1.8.8
- Bugfix
    - Fix total token supply

## 1.8.1
- Added mapping for chain update events
    - `MinBlockTimeUpdate`
    - `TimeoutParametersUpdate`
    - `FinalizationCommitteeParametersUpdate`
    - `BlockEnergyLimitUpdate`
    - `GasRewardsCpv2Update`

## 1.8.0
- Added current payday commissions for validators such that pending commission changes can be identified.

## 1.7.9
- Added functionality to sort validators based on their block commissions.

## 1.7.8
- Added
    - Deserialization of events and parameters for contract (rejected) events where a module schema is present.
- Updated
    - Modified module queries to use Dapper instead of Entity Framework to improve performance.
    
## 1.7.7
- Added
    - Endpoint to display module schema in a human interpretable form. The schema is present if it is embedded in the module source.
- Bugfix
    - Schema was incorrectly always mapped to undefined schema version. Fix implemented and job added which cleans up corrupted data.

## 1.7.4
- Change contract- and module queries to use offset pagination.

## 1.7.2
- Added feature which fetched Contract data from node and store as events.
- Added Prometheus metrics
    - Durations of imports, count of relevant processed transaction events and retry counts.
    - Duration of GraphQL endpoints
- Added health checks, where application goes into degraded state if some job stops running. These health checks are now making endpoint `rest/health` redundant and it is removed.
- Added GraphQL endpoints
    - Contract listing with pagination and details page.
    - Module details page.
- Extended search functionality to include contracts and modules.
- Updated
    - `HotChocolate` nuget libraries from major `12` to `13`. Because of this done some migration changes like adding `RegisterDbContext<GraphQlDbContext>(DbContextKind.Pooled)` 
    to GraphQL schema configuration *(migration documentation located at: https://chillicream.com/docs/hotchocolate/v13/migrating/migrate-from-12-to-13)*.

## 1.6.3
- Added Account Balance to Account Statement export file.

## 1.6.2
- Bugfix
    - Added as before protocol update two transaction events, `CredentialDeployed` and `AccountCreated`, when a account is created.

## 1.6.1
- Bugfix
    - Bumped NET SDK to 4.0.1 which fixes wrong transaction count mapping.
    - Fixed Passive Delegation queries to be robust with chain parameters level 2.
    - Fixed changed types on table `metrics_payday_pool_rewards` from PostgreSQL `numeric` to `double` such that they can't be casted to C# `double`s.

## 1.6.0
- Added support for CIS Token balances listing under accounts sidebar
- Handled exceptions when Baker & Account not available in Database while trying to update them.
- Added support for Contract Upgraded Event (Protocol 5)
- Reverted catching of all possible Baker Update Exceptions and moved logic to update pool status after a baker has been added.
- Handling of block `1385825` on testnet. Which changes the NextPaydayTime by just 287 seconds.
- Added query to fetch backend version.
- Sorting on Nodes Page
- Added Accounts Updated GraphQl subscription. Can be used to subscribe to updates of a specific Account. 
- Fixed Issue 5 by considering 0 balance for Newly created accounts.
- Added boolean filters on Bakers Query to enable filtering on Baker Removed State.
- Fixed Memo CBOR Decoding.
- Changed the header from "Total circulating supply of CCD" to "Total unlocked CCD"
- Added New API endpoint to get "unlocked CCD"
- Fixed Issue 55 by setting account balance to actual queried balance in case of genesis block.
- Issue 62 : Fixed display of tokens using numeric display style and metadata dependent decimals
- Issue 22 :  Account sidebar will always load account from backend and not use local cache
- Issue 61 :  Changed token supply to BigInteger from double causing the issue of overflow
- Issue 7 : Baker sidebar now shows actual removal time in popup and not effective time
- Issue 75 : Added Total Amount Released rest API endpoint
- Issue 69 : Amount component now handles negative values properly
