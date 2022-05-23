import type { BakerPoolOpenStatus } from '~/types/generated'

const translations = {
	entryTypes: {
		CLOSED_FOR_ALL: 'closed for all',
		OPEN_FOR_ALL: 'open for all',
		CLOSED_FOR_NEW: 'closed for new',
	} as Record<BakerPoolOpenStatus | 'UNKNOWN', string>,
}
export const translateBakerPoolOpenStatus = (
	bakerPoolOpenStatus: BakerPoolOpenStatus
) => {
	const translationKey = bakerPoolOpenStatus || 'UNKNOWN'
	return (
		translations.entryTypes[translationKey] || translations.entryTypes.UNKNOWN
	)
}
