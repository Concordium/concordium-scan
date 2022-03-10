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
export const prettyFormatBucketDuration = (d: string) => {
	const durationObject = duration.parse(d)
	if (durationObject.days)
		return durationObject.days + (durationObject.days > 1 ? ' days' : ' day')
	if (durationObject.minutes)
		return (
			durationObject.minutes +
			(durationObject.minutes > 1 ? ' minutes' : ' minute')
		)
	if (durationObject.hours)
		return (
			durationObject.hours + (durationObject.hours > 1 ? ' hours' : ' hour')
		)
	if (durationObject.seconds)
		return (
			durationObject.seconds +
			(durationObject.seconds > 1 ? ' seconds' : ' second')
		)
	if (durationObject.years)
		return (
			durationObject.years + (durationObject.years > 1 ? ' years' : ' year')
		)
}

/**
 * Formats timestamp using browser locale
 * @param {string} timestamp - ISO string
 * @returns {string} - Nicely formatted string
 * @example
 * // returns "Jul 20, 1969, 8:17 PM"
 * formatTimestamp(1969-07-20T20:17:40.000Z);
 */
export const formatTimestamp = (timestamp: string) => {
	const options = {
		year: 'numeric',
		month: 'short',
		day: 'numeric',
		hour: 'numeric',
		minute: 'numeric',
	} as Intl.DateTimeFormatOptions

	return new Intl.DateTimeFormat('default', options).format(new Date(timestamp))
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
	new Intl.NumberFormat(undefined, { minimumFractionDigits: 6 }).format(
		amount / 1_000_000
	)

/**
 * Formats a number to browser locale (with thousand separators and decimal)
 * @param {number} number - Any number
 * @returns {string} - Formatted number
 * @example
 * // in en-US (standard)
 * // returns 1,337.42
 * convertMicroCcdToCcd(1337.42);
 */
export const formatNumber = (num = 0): string =>
	new Intl.NumberFormat().format(num)

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

	return new Intl.NumberFormat(undefined, {
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
