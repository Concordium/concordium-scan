import type { BakerRewardType } from '~/types/generated'

const translations = {
	entryTypes: {
		FOUNDATION_REWARD: 'Foundation reward',
		BAKER_REWARD: 'Baker reward',
		FINALIZATION_REWARD: 'Finalization reward',
		TRANSACTION_FEE_REWARD: 'Transaction fee reward',
		UNKNOWN: 'Unknown',
	} as Record<BakerRewardType | 'UNKNOWN', string>,
}
export const translateBakerRewardType = (
	accountStatementEntry: BakerRewardType
) => {
	const translationKey = accountStatementEntry || 'UNKNOWN'
	return (
		translations.entryTypes[translationKey] || translations.entryTypes.UNKNOWN
	)
}
