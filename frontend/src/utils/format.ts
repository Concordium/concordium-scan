import {
	addMilliseconds,
	formatDistance,
	parseISO,
	subMilliseconds,
} from 'date-fns'

import * as duration from 'duration-fns'
import type { UnwrapRef } from 'vue'
import { decode, diagnose, encode } from 'cbor2'

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

// Cache for scale factors to avoid repeated BigInt exponentiations
const scaleFactorCache = new Map<number, bigint>()

/**
 * Calculates actual value by addressing decimals while preserving precision
 * This function converts a token amount from its smallest unit to its display unit
 * while maintaining precision for BigInt arithmetic operations.
 *
 * @param value - The raw token amount as a string
 * @param decimals - Number of decimal places for the token (can be up to 255)
 * @returns The actual value scaled to a common precision for arithmetic
 */
export const calculateActualValue = (
	value: string,
	decimals: number
): bigint => {
	const bigIntValue = BigInt(value)

	// For tokens with different decimal places, we need to normalize them
	// to a common scale for proper addition. We'll use 255 decimals as the maximum
	const COMMON_DECIMALS = 255

	switch (true) {
		case decimals === COMMON_DECIMALS:
			// Already at common scale
			return bigIntValue

		case decimals < COMMON_DECIMALS: {
			// Scale up to common decimals efficiently with caching
			const scaleDiff = COMMON_DECIMALS - decimals

			// Check cache first to avoid repeated expensive calculations
			let scaleUp = scaleFactorCache.get(scaleDiff)
			if (!scaleUp) {
				scaleUp = BigInt(10) ** BigInt(scaleDiff)
				scaleFactorCache.set(scaleDiff, scaleUp)
			}

			return bigIntValue * scaleUp
		}

		default:
			// This case shouldn't happen since 255 is the maximum, but handle it just in case
			console.warn(
				`Token has more than ${COMMON_DECIMALS} decimals: ${decimals}`
			)
			return bigIntValue
	}
}

/**
 * Calculates percentage for BigInt values with 2 decimal precision
 *
 * Since BigInt division truncates decimals,this is a scaling technique:
 * 1. Multiply numerator by 10000 to preserve 4 decimal places in division
 * 2. Split result into integer and fractional parts
 * 3. Format as percentage with 2 decimal places
 *
 * @param value - The numerator value as BigInt
 * @param total - The denominator value as BigInt
 * @returns Percentage as string(bigint can not represent decimals) with 2 decimal places (e.g., "12.34")
 *
 * @example
 * calculatePercentageforBigInt(25n, 100n) // returns "25.00"
 * calculatePercentageforBigInt(1n, 3n)    // returns "33.33"
 * calculatePercentageforBigInt(1n, 7n)    // returns "14.28"
 */
export const calculatePercentageforBigInt = (
	value: bigint,
	total: bigint
): string => {
	// Scale up by 10000 to preserve 4 decimal places during BigInt division
	// This allows us to extract 2 decimal places for percentage display
	const scaledResult = (value * BigInt(10000)) / total

	// Convert to string to manually extract integer and decimal parts
	const resultString = scaledResult.toString()
	const DECIMAL_PLACES = 2

	// Split the scaled result into integer and decimal parts
	if (resultString.length <= DECIMAL_PLACES) {
		// Handle small numbers: pad with leading zeros for decimal part
		const integerPart = 0
		const decimalPart = resultString.padStart(DECIMAL_PLACES, '0')
		return `${integerPart}.${decimalPart}`
	} else {
		// Handle normal numbers: split at decimal position
		const integerPart = resultString.slice(0, -DECIMAL_PLACES)
		const decimalPart = resultString.slice(-DECIMAL_PLACES)
		return `${integerPart}.${decimalPart}`
	}
}

/**
 * Validates if a string represents valid hexadecimal data
 * Removes '0x' prefix if present and checks for valid hex characters and even length
 * @param str - The string to validate as hex
 * @returns True if the string is valid hex, false otherwise
 */
export const isValidHex = (str: string): boolean => {
	const cleanHex = str.startsWith('0x') ? str.slice(2) : str
	return /^[0-9a-fA-F]*$/.test(cleanHex) && cleanHex.length % 2 === 0
}

/**
 * Validates and cleans a hex string, returning the cleaned hex or null if invalid
 * @param hex - The hex string to validate and clean
 * @returns Cleaned hex string without '0x' prefix, or null if invalid
 */
const validateAndCleanHex = (hex: string): string | null => {
	if (!hex || hex.trim() === '') {
		return null
	}

	const cleanHex = hex.startsWith('0x') ? hex.slice(2) : hex

	if (!isValidHex(cleanHex) || cleanHex.length < 2) {
		return null
	}

	return cleanHex
}

/**
 * Converts a validated hex string to a Uint8Array byte array
 * @param cleanHex - The validated and cleaned hex string
 * @returns Uint8Array of bytes
 */
const hexToBytes = (cleanHex: string): Uint8Array => {
	const bytes = new Uint8Array(cleanHex.length / 2)
	for (let i = 0; i < bytes.length; i++) {
		const byteStr = cleanHex.substr(i * 2, 2)
		const byteVal = parseInt(byteStr, 16)
		if (isNaN(byteVal)) {
			throw new Error(`Invalid hex byte at position ${i}: ${byteStr}`)
		}
		bytes[i] = byteVal
	}
	return bytes
}

/**
 * Attempts to decode CBOR data and return formatted result
 * @param bytes - The byte array to decode
 * @param useDiagnostic - Whether to use diagnostic notation
 * @returns Decoded result or error marker
 */
const decodeCborBytes = (bytes: Uint8Array, useDiagnostic: boolean): string => {
	try {
		if (useDiagnostic) {
			return diagnose(bytes)
		} else {
			const decoded = decode(bytes)
			return JSON.stringify(decoded, null, 2)
		}
	} catch (error) {
		const errorMessage = error instanceof Error ? error.message : String(error)
		const errorPrefix = useDiagnostic
			? '__CBOR_DIAGNOSTIC_ERROR__'
			: '__CBOR_DECODE_ERROR__'
		return `${errorPrefix}${errorMessage}`
	}
}

/**
 * Determines if decoded text appears to be binary data rather than readable text
 * Analyzes character distribution to detect non-printable content
 * @param str - The decoded string to analyze
 * @returns True if the string appears to be binary data (less than 70% printable chars)
 */
export const isLikelyBinary = (str: string): boolean => {
	// Count printable vs non-printable characters
	let printable = 0
	let total = 0

	for (let i = 0; i < str.length; i++) {
		const charCode = str.charCodeAt(i)
		total++
		// Consider printable: ASCII 32-126, tabs, newlines, carriage returns
		if (
			(charCode >= 32 && charCode <= 126) ||
			charCode === 9 || // tab
			charCode === 10 || // newline
			charCode === 13 // carriage return
		) {
			printable++
		}
	}

	// If less than 70% printable and string is long enough, likely binary
	return total > 10 && printable / total < 0.7
}

/**
 * Safely truncates very long strings to prevent UI performance issues
 * Adds a truncation indicator showing how many characters were removed
 * @param str - The string to potentially truncate
 * @param maxLength - Maximum allowed length before truncation (default: 10000)
 * @returns Original string if short enough, or truncated version with indicator
 */
export const safeTruncate = (
	str: string,
	maxLength: number = 10000
): string => {
	if (str.length <= maxLength) return str

	const truncated = str.substring(0, maxLength)
	return `${truncated}... [${str.length - maxLength} more characters truncated]`
}

/**
 * Converts hexadecimal string to human-readable text with comprehensive error handling
 * Handles validation, decoding, binary detection, and safe truncation
 * @param hex - The hexadecimal string to convert (may include '0x' prefix)
 * @returns Readable UTF-8 text, binary data indicator, or error message
 */
export const hexToString = (hex: string): string => {
	try {
		// Handle empty/null input
		if (!hex || hex.trim() === '') {
			return '(empty hex data)'
		}

		// Validate hex format before processing
		if (!isValidHex(hex)) {
			return `(invalid hex format: ${hex.substring(0, 50)}${
				hex.length > 50 ? '...' : ''
			})`
		}

		const cleanHex = hex.startsWith('0x') ? hex.slice(2) : hex

		// Handle incomplete hex data
		if (cleanHex.length < 2) {
			return `(incomplete hex: ${cleanHex})`
		}

		// Convert hex string to byte array
		const bytes = new Uint8Array(cleanHex.length / 2)
		for (let i = 0; i < bytes.length; i++) {
			const byteStr = cleanHex.substr(i * 2, 2)
			const byteVal = parseInt(byteStr, 16)
			if (isNaN(byteVal)) {
				return `(invalid hex byte at position ${i}: ${byteStr})`
			}
			bytes[i] = byteVal
		}

		// Decode bytes as UTF-8 text
		const decoder = new TextDecoder('utf-8', { fatal: false })
		const decodedText = decoder.decode(bytes)

		// Check if the result appears to be binary data
		if (isLikelyBinary(decodedText)) {
			// For binary data, show hex representation instead
			return `(binary data: ${cleanHex.substring(0, 100)}${
				cleanHex.length > 100 ? '...' : ''
			})`
		}

		// Return safely truncated readable text
		return safeTruncate(decodedText)
	} catch (error) {
		console.warn('Hex to string conversion failed:', error)
		return `(conversion error: ${hex.substring(0, 50)}${
			hex.length > 50 ? '...' : ''
		})`
	}
}

/**
 * Intelligently formats hex data by detecting if it's actually CBOR-encoded
 * This is the main logic for handling data marked as 'HEX' type:
 * 1. Validates hex format
 * 2. Attempts CBOR decoding
 * 3. If CBOR decoding produces structured data, displays as JSON or diagnostic notation
 * 4. Otherwise falls back to text conversion
 * @param hex - The hex string that might be CBOR-encoded data
 * @param useDiagnostic - Whether to use CBOR diagnostic notation instead of JSON
 * @returns Formatted JSON/diagnostic if CBOR detected, readable text if plain hex, or error message
 */
export const formatHexData = (hex: string, useDiagnostic = false): string => {
	try {
		// Handle empty input
		if (!hex || hex.trim() === '') {
			return '(empty hex data)'
		}

		// Early validation of hex format
		if (!isValidHex(hex)) {
			return `(invalid hex format: ${safeTruncate(hex, 100)})`
		}

		const cleanHex = hex.startsWith('0x') ? hex.slice(2) : hex

		// Handle incomplete hex
		if (cleanHex.length < 2) {
			return `(incomplete hex: ${cleanHex})`
		}

		// Attempt CBOR decoding first
		const cborResult = useDiagnostic
			? decodeCborHexDiagnostic(cleanHex)
			: decodeCborHex(cleanHex)

		// Check if CBOR decoding succeeded (not an error)
		const errorPrefix = useDiagnostic
			? '__CBOR_DIAGNOSTIC_ERROR__'
			: '__CBOR_DECODE_ERROR__'
		if (!cborResult.startsWith(errorPrefix)) {
			try {
				if (!useDiagnostic) {
					const parsed = JSON.parse(cborResult)
					// If decoding produced structured data (object/array), it's likely CBOR
					if (typeof parsed === 'object' && parsed !== null) {
						// Return formatted JSON, safely truncated for performance
						return safeTruncate(cborResult, 5000)
					}
				} else {
					// For diagnostic format, check if it looks like diagnostic notation
					// Diagnostic notation typically contains brackets, braces, or specific CBOR syntax
					if (
						cborResult.includes('[') ||
						cborResult.includes('{') ||
						cborResult.includes("h'") ||
						cborResult.includes('<<')
					) {
						return safeTruncate(cborResult, 5000)
					}
				}
			} catch {
				// JSON parsing failed, not structured CBOR data
			}
		}

		// Fall back to standard hex-to-text conversion
		return hexToString(hex)
	} catch (error) {
		console.warn('HEX data formatting failed:', error)
		// Return safe truncated version with error indication
		return `(formatting error: ${safeTruncate(hex, 100)})`
	}
}

/**
 * Decodes CBOR-encoded hex data to JSON string using cbor2 library
 * cbor2 is a fast, RFC 8949 compliant CBOR decoder
 * @param hex - The CBOR-encoded hexadecimal string
 * @returns JSON string representation of decoded CBOR data, or error marker
 */
export const decodeCborHex = (hex: string): string => {
	try {
		const cleanHex = validateAndCleanHex(hex)
		if (!cleanHex) {
			return '(empty CBOR data)'
		}

		const bytes = hexToBytes(cleanHex)
		return decodeCborBytes(bytes, false)
	} catch (error) {
		console.warn('CBOR decoding failed:', error)
		const errorMessage = error instanceof Error ? error.message : String(error)
		return `__CBOR_DECODE_ERROR__${errorMessage}`
	}
}

/**
 * Decodes CBOR-encoded hex data to diagnostic notation using cbor2 library
 * CBOR diagnostic notation provides human-readable representation of CBOR data structures
 * @param hex - The CBOR-encoded hexadecimal string
 * @returns CBOR diagnostic notation string, or error marker
 */
export const decodeCborHexDiagnostic = (hex: string): string => {
	try {
		const cleanHex = validateAndCleanHex(hex)
		if (!cleanHex) {
			return '(empty CBOR data)'
		}

		const bytes = hexToBytes(cleanHex)
		return decodeCborBytes(bytes, true)
	} catch (error) {
		console.warn('CBOR diagnostic decoding failed:', error)
		const errorMessage = error instanceof Error ? error.message : String(error)
		return `__CBOR_DIAGNOSTIC_ERROR__${errorMessage}`
	}
}

/**
 * Formats CBOR data for display, handling both pre-decoded JSON and raw CBOR hex
 * This is the main logic for handling data marked as 'CBOR' type:
 * 1. Try to parse as already-decoded JSON
 * 2. If that fails, try to decode as CBOR hex
 * 3. Format appropriately or show error
 * @param text - The CBOR data (either JSON string or CBOR hex)
 * @param useDiagnostic - Whether to use CBOR diagnostic notation instead of JSON
 * @returns Formatted JSON/diagnostic for display, or error message
 */
export const formatCborData = (text: string, useDiagnostic = false): string => {
	try {
		// Handle empty input
		if (!text || text.trim() === '') {
			return '(empty CBOR data)'
		}

		// First attempt: parse as already-decoded JSON
		try {
			const parsed = JSON.parse(text)
			// Ensure it's structured data (object or array)
			if (typeof parsed === 'object' && parsed !== null) {
				// Return pretty-printed JSON or convert to diagnostic notation
				if (useDiagnostic) {
					// Convert JSON back to CBOR bytes and then to diagnostic
					const cborBytes = encode(parsed)
					return diagnose(cborBytes)
				} else {
					return safeTruncate(JSON.stringify(parsed, null, 2), 5000)
				}
			}
			// Handle primitive values
			if (useDiagnostic) {
				// For primitives, convert to CBOR and then to diagnostic
				const cborBytes = encode(parsed)
				return diagnose(cborBytes)
			} else {
				return JSON.stringify(parsed, null, 2)
			}
		} catch {
			// Not valid JSON, check if it's hex-encoded CBOR
			if (isValidHex(text)) {
				const cborResult = useDiagnostic
					? decodeCborHexDiagnostic(text)
					: decodeCborHex(text)
				// Check if decoding succeeded
				const errorPrefix = useDiagnostic
					? '__CBOR_DIAGNOSTIC_ERROR__'
					: '__CBOR_DECODE_ERROR__'
				if (!cborResult.startsWith(errorPrefix)) {
					return cborResult
				}
			}
			// Neither JSON nor valid CBOR hex
			return `(unrecognized CBOR format: ${safeTruncate(text, 100)})`
		}
	} catch (error) {
		console.warn('CBOR data formatting failed:', error)
		return `(CBOR formatting error: ${safeTruncate(text, 100)})`
	}
}
