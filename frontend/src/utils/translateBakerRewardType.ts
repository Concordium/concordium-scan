import type { BakerRewardType } from '~/types/generated'
import { AccountStatementEntryType, RewardType } from '~/types/generated'

const translations = {
	entryTypes: {
		FOUNDATION_REWARD: 'Foundation reward',
		BAKER_REWARD: 'Baker reward',
		FINALIZATION_REWARD: 'Finalization reward',
		TRANSACTION_FEE_REWARD: 'Transaction fee reward',
		UNKNOWN: 'Unknown',
	} as Record<
		BakerRewardType | AccountStatementEntryType | RewardType | 'UNKNOWN',
		string
	>,
}
export const translateBakerRewardType = (
	accountStatementEntry:
		| BakerRewardType
		| AccountStatementEntryType
		| RewardType
) => {
	const translationKey = accountStatementEntry || 'UNKNOWN'
	return (
		translations.entryTypes[translationKey] || translations.entryTypes.UNKNOWN
	)
}
