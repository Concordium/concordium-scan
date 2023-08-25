## Unreleased changes
- Added feature which fetched Smart Contract data from node and store as events.
- Added Prometheus metrics which give durations of imports, count of relevant processed transaction events and retry counts.
- Added health checks, where application goes into degraded state if some job stops running. These health checks are now making endpoint `rest/health` redundant and it is removed.

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
