import type { AccountStatementEntryType } from '~/types/generated'

const translations = {
	entryTypes: {
		AMOUNT_DECRYPTED: 'Decrypted',
		AMOUNT_ENCRYPTED: 'Encrypted',
		BAKING_REWARD: 'Reward',
		BLOCK_REWARD: 'Reward',
		FINALIZATION_REWARD: 'Reward',
		MINT_REWARD: 'Reward',
		TRANSACTION_FEE: 'Fee',
		TRANSFER_IN: 'Transfer',
		TRANSFER_OUT: 'Transfer',
		UNKNOWN: 'Unknown',
	},
}
export const translateAccountStatementEntryType = (
	accountStatementEntry: AccountStatementEntryType
) => {
	const translationKey = accountStatementEntry || 'UNKNOWN'
	return (
		translations.entryTypes[translationKey] || translations.entryTypes.UNKNOWN
	)

	return 'Unknown'
}
