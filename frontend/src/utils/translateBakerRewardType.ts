import type { AccountStatementEntryType, RewardType } from '~/types/generated'

const translations = {
	entryTypes: {
		FOUNDATION_REWARD: 'Foundation reward',
		BAKER_REWARD: 'Validator reward',
		FINALIZATION_REWARD: 'Finalization reward',
		TRANSACTION_FEE_REWARD: 'Transaction fee reward',
		UNKNOWN: 'Unknown',
	} as Record<AccountStatementEntryType | RewardType | 'UNKNOWN', string>,
}

export const translateBakerRewardType = (
	accountStatementEntry: AccountStatementEntryType | RewardType
) => {
	const translationKey = accountStatementEntry || 'UNKNOWN'
	return (
		translations.entryTypes[translationKey] || translations.entryTypes.UNKNOWN
	)
}
