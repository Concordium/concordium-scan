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

type CredentialDeploymentTypes = 'INITIAL' | 'NORMAL'

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

export type AccountTransaction = {
	__typename: 'AccountTransaction'
	accountTransactionType: AccountTransactionTypes
}

export type CredentialDeploymentTransaction = {
	__typename: 'CredentialDeploymentTransaction'
	credentialDeploymentTransactionType: CredentialDeploymentTypes
}

export type UpdateTransaction = {
	__typename: 'UpdateTransaction'
	updateTransactionType: UpdateTransactionTypes
}

export type TransactionType =
	| AccountTransaction
	| UpdateTransaction
	| CredentialDeploymentTransaction

const translations = {
	accountTransactionTypes: {
		DEPLOY_MODULE: 'Deploy module',
		INITIALIZE_SMART_CONTRACT_INSTANCE: 'Initialise Smart Contract',
		UPDATE_SMART_CONTRACT_INSTANCE: 'Update Smart Contract',
		SIMPLE_TRANSFER: 'Simple transfer',
		ADD_BAKER: 'Add baker',
		REMOVE_BAKER: 'Remove baker',
		UPDATE_BAKER_STAKE: 'Update baker stake',
		UPDATE_BAKER_RESTAKE_EARNINGS: 'Update baker restake earnings',
		UPDATE_BAKER_KEYS: 'Update baker keys',
		UPDATE_CREDENTIAL_KEYS: 'Update credential keys',
		ENCRYPTED_TRANSFER: 'Encrypted transfer',
		TRANSFER_TO_ENCRYPTED: 'Transfer to encrypted',
		TRANSFER_TO_PUBLIC: 'Transfer to public',
		TRANSFER_WITH_SCHEDULE: 'Transfer with schedule',
		UPDATE_CREDENTIALS: 'Update credentials',
		REGISTER_DATA: 'Register data',
		SIMPLE_TRANSFER_WITH_MEMO: 'Simple transfer with memo',
		ENCRYPTED_TRANSFER_WITH_MEMO: 'Encrypted transfer with memo',
		TRANSFER_WITH_SCHEDULE_WITH_MEMO: 'Transfer with schedule and memo',
	},
	credentialDeploymentTypes: {
		NORMAL: 'Normal credential deployment',
		INITIAL: 'Initial credential deployment',
	},
	updateTransactionTypes: {
		UPDATE_PROTOCOL: 'Update protocol',
		UPDATE_ELECTION_DIFFICULTY: 'Update election difficulty',
		UPDATE_EURO_PER_ENERGY: 'Update Euro per Energy',
		UPDATE_MICRO_GTU_PER_EURO: 'Update micro CCD per Euro',
		UPDATE_FOUNDATION_ACCOUNT: 'Update foundation account',
		UPDATE_MINT_DISTRIBUTION: 'Update mint distribution',
		UPDATE_TRANSACTION_FEE_DISTRIBUTION: 'Update transaction fee distribution',
		UPDATE_GAS_REWARDS: 'Update gas rewards',
		UPDATE_BAKER_STAKE_THRESHOLD: 'Update baker stake threshold',
		UPDATE_ADD_ANONYMITY_REVOKER: 'Add anonymity revoker',
		UPDATE_ADD_IDENTITY_PROVIDER: 'Add identity provider',
		UPDATE_ROOT_KEYS: 'Update root keys',
		UPDATE_LEVEL1_KEYS: 'Update level1 keys',
		UPDATE_LEVEL2_KEYS: 'Update level2 keys',
	},
}

export const translateTransactionType = (txType: TransactionType) => {
	if (txType.__typename === 'AccountTransaction') {
		return (
			translations.accountTransactionTypes[txType.accountTransactionType] ||
			'Unknown account transaction'
		)
	}

	if (txType.__typename === 'CredentialDeploymentTransaction') {
		return (
			translations.credentialDeploymentTypes[
				txType.credentialDeploymentTransactionType
			] || 'Unknown credential deployment'
		)
	}

	if (txType.__typename === 'UpdateTransaction') {
		return (
			translations.updateTransactionTypes[txType.updateTransactionType] ||
			'Unknown update transaction'
		)
	}

	return 'Unknown transaction type'
}
