import type { TransactionType } from '~/types/generated'

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
		UNKNOWN: 'Unknown account transaction',
	},
	credentialDeploymentTypes: {
		NORMAL: 'Normal credential deployment',
		INITIAL: 'Initial credential deployment',
		UNKNOWN: 'Unknown credential deployment',
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
		UNKNOWN: 'Unknown update transaction',
	},
}

export const translateTransactionType = (txType: TransactionType) => {
	if (txType.__typename === 'AccountTransaction') {
		const translationKey = txType.accountTransactionType || 'UNKNOWN'
		return (
			translations.accountTransactionTypes[translationKey] ||
			translations.accountTransactionTypes.UNKNOWN
		)
	}

	if (txType.__typename === 'CredentialDeploymentTransaction') {
		const translationKey =
			txType.credentialDeploymentTransactionType || 'UNKNOWN'
		return (
			translations.credentialDeploymentTypes[translationKey] ||
			translations.credentialDeploymentTypes.UNKNOWN
		)
	}

	if (txType.__typename === 'UpdateTransaction') {
		const translationKey = txType.updateTransactionType || 'UNKNOWN'
		return (
			translations.updateTransactionTypes[translationKey] ||
			translations.updateTransactionTypes.UNKNOWN
		)
	}

	return 'Unknown transaction type'
}
