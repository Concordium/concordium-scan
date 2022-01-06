import { formatDistance, parseISO } from 'date-fns'

export const convertTimestampToRelative = (
	timestamp: string,
	compareDate: Date = new Date()
) =>
	formatDistance(parseISO(timestamp), compareDate, {
		addSuffix: true,
	})

/**
 * Converts microCCD to CCD with fixed decimals
 * @constructor
 * @param {number} number - Value in microCCD
 * @returns {string} - Value in CCD
 * @example
 * // returns 0.001337
 * convertMicroCcdToCcd(1337);
 */
export const convertMicroCcdToCcd = (amount = 0): string =>
	new Intl.NumberFormat('en-GB', { minimumFractionDigits: 6 }).format(
		amount / 1_000_000
	)
