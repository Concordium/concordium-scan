import type { BakerPoolOpenStatus } from '~/types/generated'

const translations = {
	entryTypes: {
		CLOSED_FOR_ALL: 'Closed for all',
		OPEN_FOR_ALL: 'Open for all',
		CLOSED_FOR_NEW: 'Closed for new',
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
