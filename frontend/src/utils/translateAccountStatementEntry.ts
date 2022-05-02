import type { AccountStatementEntryType } from '~/types/generated'

const translations = {
	entryTypes: {
		AMOUNT_DECRYPTED: 'Decrypted',
		AMOUNT_ENCRYPTED: 'Encrypted',
		BAKER_REWARD: 'Reward',
		FINALIZATION_REWARD: 'Reward',
		FOUNDATION_REWARD: 'Reward',
		TRANSACTION_FEE: 'Fee',
		TRANSACTION_FEE_REWARD: 'Reward',
		TRANSFER_IN: 'Transfer',
		TRANSFER_OUT: 'Transfer',
		UNKNOWN: 'Unknown',
	} as Record<AccountStatementEntryType | 'UNKNOWN', string>,
}
export const translateAccountStatementEntryType = (
	accountStatementEntry: AccountStatementEntryType
) => {
	const translationKey = accountStatementEntry || 'UNKNOWN'
	return (
		translations.entryTypes[translationKey] || translations.entryTypes.UNKNOWN
	)
}
