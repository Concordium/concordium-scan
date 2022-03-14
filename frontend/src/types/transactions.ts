import type { TransactionResult, Block } from '~/types/generated'

/** @deprecated Use generated type "AccountTransactionType" instead */
type AccountTransactionTypes =
	| 'DEPLOY_MODULE'
	| 'INITIALIZE_SMART_CONTRACT_INSTANCE'
	| 'UPDATE_SMART_CONTRACT_INSTANCE'
	| 'SIMPLE_TRANSFER'
	| 'ADD_BAKER'
	| 'REMOVE_BAKER'
	| 'UPDATE_BAKER_STAKE'
	| 'UPDATE_BAKER_RESTAKE_EARNINGS'
	| 'UPDATE_BAKER_KEYS'
	| 'UPDATE_CREDENTIAL_KEYS'
	| 'ENCRYPTED_TRANSFER'
	| 'TRANSFER_TO_ENCRYPTED'
	| 'TRANSFER_TO_PUBLIC'
	| 'TRANSFER_WITH_SCHEDULE'
	| 'UPDATE_CREDENTIALS'
	| 'REGISTER_DATA'
	| 'SIMPLE_TRANSFER_WITH_MEMO'
	| 'ENCRYPTED_TRANSFER_WITH_MEMO'
	| 'TRANSFER_WITH_SCHEDULE_WITH_MEMO'

/** @deprecated Use generated type "CredentialDeploymentTransactionType" instead */
type CredentialDeploymentTypes = 'INITIAL' | 'NORMAL'

/** @deprecated Use generated type "UpdateTransactionType" instead */
type UpdateTransactionTypes =
	| 'UPDATE_PROTOCOL'
	| 'UPDATE_ELECTION_DIFFICULTY'
	| 'UPDATE_EURO_PER_ENERGY'
	| 'UPDATE_MICRO_GTU_PER_EURO'
	| 'UPDATE_FOUNDATION_ACCOUNT'
	| 'UPDATE_MINT_DISTRIBUTION'
	| 'UPDATE_TRANSACTION_FEE_DISTRIBUTION'
	| 'UPDATE_GAS_REWARDS'
	| 'UPDATE_BAKER_STAKE_THRESHOLD'
	| 'UPDATE_ADD_ANONYMITY_REVOKER'
	| 'UPDATE_ADD_IDENTITY_PROVIDER'
	| 'UPDATE_ROOT_KEYS'
	| 'UPDATE_LEVEL1_KEYS'
	| 'UPDATE_LEVEL2_KEYS'

/** @deprecated Use generated type "AccountTransaction" instead */
export type AccountTransaction = {
	__typename: 'AccountTransaction'
	accountTransactionType: AccountTransactionTypes
}

/** @deprecated Use generated type "CredentialDeploymentTransaction" instead */
export type CredentialDeploymentTransaction = {
	__typename: 'CredentialDeploymentTransaction'
	credentialDeploymentTransactionType: CredentialDeploymentTypes
}

/** @deprecated Use generated type "UpdateTransaction" instead */
export type UpdateTransaction = {
	__typename: 'UpdateTransaction'
	updateTransactionType: UpdateTransactionTypes
}

/** @deprecated Use generated type "TransactionType" instead */
export type TransactionType =
	| AccountTransaction
	| UpdateTransaction
	| CredentialDeploymentTransaction

/** @deprecated Use generated type "AccountAddress" instead (beware of incoming breaking change!) */
type AccountAddress = {
	__typename: 'AccountAddress'
	address: string
}

/** @deprecated Use generated type "ContractAddress" instead (beware of incoming breaking change!) */
type ContractAddress = {
	__typename: 'ContractAddress'
	index: number
	subIndex: string
}

/** @deprecated Use generated type "Address" instead */
export type TransferAddress = AccountAddress | ContractAddress

/** @deprecated Use generated type "Transaction" instead */
export type Transaction = {
	id: string
	transactionHash: string
	senderAccountAddress: string
	ccdCost: number
	block: Block
	result: TransactionResult
	transactionType: TransactionType
}
