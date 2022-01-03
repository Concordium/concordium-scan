import { formatDistance, parseISO } from 'date-fns'

export const convertTimestampToRelative = (
	timestamp: string,
	compareDate: Date = new Date()
) =>
	formatDistance(parseISO(timestamp), compareDate, {
		addSuffix: true,
	})
