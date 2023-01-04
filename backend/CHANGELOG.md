## Unreleased
- Added support for CIS Token balances listing under accounts sidebar
- Handled exceptions when Baker & Account not available in Database while trying to update them.
- Added support for Contract Upgraded Event (Protocol 5)
- Reverted catching of all possible Baker Update Exceptions and moved logic to update pool status after a baker has been added.
- Handling of block `1385825` on testnet. Which changes the NextPaydayTime by just 287 seconds.
- Added query to fetch backend version.
- Sorting on Nodes Page
- Fixed Memo CBOR Decoding.