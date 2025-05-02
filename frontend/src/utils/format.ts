import {
	addMilliseconds,
	formatDistance,
	parseISO,
	subMilliseconds,
} from 'date-fns'

import * as duration from 'duration-fns'
import type { UnwrapRef } from 'vue'

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

/**
 * Formats timestamp using browser locale
 * @param {string} timestamp - ISO string
 * @returns {string} - Nicely formatted string
 * @example
 * // returns "Jul 20, 1969, 8:17 PM"
 * formatTimestamp(1969-07-20T20:17:40.000Z);
 */
export const formatShortTimestamp = (timestamp: string) => {
	const options = {
		year: 'numeric',
		month: 'numeric',
		day: 'numeric',
	} as Intl.DateTimeFormatOptions

	return new Intl.DateTimeFormat('default', options).format(new Date(timestamp))
}

/**
 * Outputs a formatted relative date comparision (e.g. 1 day ago)
 * @param {string} timestamp - Date as ISO string
 * @param {Date} compareDate - Date to compare with (defaults to days date)
 * @param {boolean} addSuffix - Whether to add relational text, e.g. "... ago" or "in ..." (defaults to false)
 * @returns {string} - Formatted relative date comparision
 * @example
 * // returns "about 22 years" (if current year is 2022)
 * convertTimestampToRelative('2000-01-01T00:00:00.000Z');
 */
export const convertTimestampToRelative = (
	timestamp: string,
	compareDate: Date = new Date(),
	addSuffix = false
) =>
	formatDistance(parseISO(timestamp), compareDate, {
		addSuffix,
	})

/**
 * Calculates effective time of the event.
 * When event effectively happens on the next payday after the event
 * @param {string} timestamp Time of the event
 * @param {string} nextPaydayTime Time of the next payday. This should be take from the most recent block ideally.
 * @returns {string} Effective time of the event. Time when event will actually take place.
 */
export const tillNextPayday = (
	timestamp: string,
	nextPaydayTime: string,
	paydayDurationMs: number
) => {
	const time = parseISO(timestamp)
	const paydayTime = parseISO(nextPaydayTime)
	if (time < paydayTime) {
		return paydayTime.toISOString()
	}

	const diffMs = time.getTime() - paydayTime.getTime()
	const diffPayDays = Math.ceil(diffMs / paydayDurationMs)
	const nextDayPayDayTime = addMilliseconds(
		paydayTime,
		diffPayDays * paydayDurationMs
	)

	return nextDayPayDayTime.toISOString()
}

/**
 * Converts microCCD to CCD with fixed decimals
 * @param {number} amount - Value in microCCD
 * @param {boolean} hideDecimals - Whether decimals should be hidden
 * @returns {string} - Value in CCD
 * @example
 * // returns 0.001337
 * convertMicroCcdToCcd(1337);
 */
export const convertMicroCcdToCcd = (
	amount: number,
	hideDecimals = false
): string =>
	// micro CCD shouldn't be fractional
	Number.isInteger(amount)
		? new Intl.NumberFormat(undefined, {
				minimumFractionDigits: hideDecimals ? 0 : 6,
				maximumFractionDigits: hideDecimals ? 0 : 6,
		  }).format(amount / 1_000_000)
		: '-'

/**
 * Formats a number to browser locale (with thousand separators and decimal)
 * @param {number} number - Any number
 * @returns {string} - Formatted number
 * @example
 * // in en-US (standard)
 * // returns 1,337.42
 * convertMicroCcdToCcd(1337.42);
 */
export const formatNumber = (num: number, decimalCount?: number): string =>
	Number.isFinite(num)
		? new Intl.NumberFormat(undefined, {
				minimumFractionDigits: decimalCount,
				maximumFractionDigits: decimalCount,
		  }).format(num)
		: '-'
/**
 * Formats an uptime to relative time
 * @param  {number} uptime - start time to calculate from
 * @param {Date} now - time now
 */
export const formatUptime = (uptime: number, now: UnwrapRef<Date>) => {
	const start = subMilliseconds(now, uptime)

	try {
		return formatDistance(start, now)
	} catch {
		return '-'
	}
}

/**
 * Calculates a weight of total in percentage
 * @param {number} amount - Single amount
 * @param {number} total - Total amount to calculate from
 * @returns {string} - Total weight in percent
 * @example
 * // returns 5
 * calculatePercentage(25, 500);
 */
export const calculatePercentage = (amount: number, total: number) =>
	(amount / total) * 100

/**
 * Shortens a hash (or any other long string)
 * @param {string} hash - String to shorten
 * @returns {string} - Shortened string
 * @example
 * // returns b4da55abc123def456
 * shortenHash(b4da55)
 */
export const shortenHash = (hash?: string) => (hash ? hash.substring(0, 6) : '')

/**
 * Formats seconds to exactly one decimal
 * @param {number} seconds - Seconds value to format
 * @returns {string} - Formatted string
 * @example
 * // returns "42.0"
 * formatSeconds(42)
 */
export const formatSeconds = (seconds: number) =>
	new Intl.NumberFormat(undefined, {
		minimumFractionDigits: 1,
		maximumFractionDigits: 1,
	}).format(seconds)

export const formatPercentage = (num: number) => {
	return new Intl.NumberFormat(undefined, {
		minimumFractionDigits: 2,
		maximumFractionDigits: 2,
	}).format(num * 100)
}

export const formatBytesPerSecond = (bytes: number) => {
	if (bytes > 1024) return (bytes / 1024).toFixed(2) + ' kB/s'
	else return bytes + ' B/s'
}

export const numberFormatter = (num?: number): string => {
	if (typeof num !== 'number' || isNaN(num)) return '0'
	return num >= 1e12
		? (num / 1e12).toFixed(2) + 'T'
		: num >= 1e9
		? (num / 1e9).toFixed(2) + 'B'
		: num >= 1e6
		? (num / 1e6).toFixed(2) + 'M'
		: num >= 1e3
		? (num / 1e3).toFixed(2) + 'K'
		: num.toFixed(2)
}

export const timeAgo = (dateString: string | undefined) => {
	if (!dateString) {
		return 'Invalid date' // or return a default value
	}
	const now = new Date()
	const past = new Date(dateString)
	const diff = Math.floor((now.getTime() - past.getTime()) / 1000) // in seconds

	if (diff < 60) return `${diff} second${diff !== 1 ? 's' : ''} ago`
	if (diff < 3600) {
		const mins = Math.floor(diff / 60)
		return `${mins} minute${mins !== 1 ? 's' : ''} ago`
	}
	if (diff < 86400) {
		const hours = Math.floor(diff / 3600)
		return `${hours} hour${hours !== 1 ? 's' : ''} ago`
	}
	const days = Math.floor(diff / 86400)
	return `${days} day${days !== 1 ? 's' : ''} ago`
}
