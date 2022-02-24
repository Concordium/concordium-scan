import { formatDistance, parseISO } from 'date-fns'

import * as duration from 'duration-fns'

/**
 * Converts an ISO 8601 duration (e.g. PT10M) to an object with the amount in years, days, months, hours, seconds, etc.
 * @param d {string} - ISO 8601 duration string.
 */
export const parseISODuration = (d: string) => {
	return duration.parse(d)
}
export const formatTimestampByBucketWidth = (
	bucketWidth: string,
	date: string
) => {
	const durationObject = parseISODuration(bucketWidth)
	if (durationObject.days && durationObject.days > 0)
		return new Date(date).toLocaleDateString()
	else if (durationObject.hours > 1) {
		return new Date(date).toLocaleString()
	} else {
		return new Date(date).toLocaleTimeString()
	}
}
export const convertTimestampToRelative = (
	timestamp: string,
	compareDate: Date = new Date()
) =>
	formatDistance(parseISO(timestamp), compareDate, {
		addSuffix: true,
	})

/**
 * Converts microCCD to CCD with fixed decimals
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

/**
 * Calculates and formats weight of total in percentage
 * @param {number} amount - Single amount
 * @param {number} total - Total amount to calculate from
 * @returns {string} - Total weight in percent
 * @example
 * // returns 5.00
 * calculateWeight(25, 500);
 */
export const calculateWeight = (amount: number, total: number) => {
	const weight = (amount / total) * 100

	return new Intl.NumberFormat('en-GB', {
		minimumFractionDigits: 2,
		maximumFractionDigits: 2,
	}).format(weight)
}

/**
 * Shortens a hash (or any other long string)
 * @param {string} hash - String to shorten
 * @returns {string} - Shortened string
 * @example
 * // returns b4da55abc123def456
 * shortenHash(b4da55)
 */
export const shortenHash = (hash?: string) => (hash ? hash.substring(0, 6) : '')
