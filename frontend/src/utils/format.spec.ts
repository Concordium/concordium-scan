import {
	convertTimestampToRelative,
	convertMicroCcdToCcd,
	calculateWeight,
	formatNumber,
	formatTimestamp,
	shortenHash,
} from './format'

describe('format', () => {
	describe('formatTimestamp', () => {
		it('should format a date to locale', () => {
			const timestamp = '1969-07-20T20:17:40.000Z'

			// assuming en-US in UTC
			expect(formatTimestamp(timestamp)).toStrictEqual('Jul 20, 1969, 8:17 PM')
		})
	})

	describe('convertTimestampToRelative', () => {
		it('should convert a timestamp into a relative time distance', () => {
			const timestamp = '2000-01-01T00:00:00.000Z'
			const compareDate = new Date(2000, 0, 11)

			const result = convertTimestampToRelative(timestamp, compareDate)

			expect(result).toStrictEqual('10 days')
		})

		it('should default the comparision date to current date', () => {
			const today = new Date()
			today.setFullYear(today.getFullYear() + 2)

			const timestamp = today.toISOString()

			const result = convertTimestampToRelative(timestamp)

			expect(result).toStrictEqual('about 2 years')
		})

		it('should show a suffix for a future date', () => {
			const today = new Date()
			today.setFullYear(today.getFullYear() + 2)

			const timestamp = today.toISOString()

			const result = convertTimestampToRelative(timestamp, undefined, true)

			expect(result).toStrictEqual('in about 2 years')
		})

		it('should show a suffix for a past date', () => {
			const today = new Date()
			today.setFullYear(today.getFullYear() - 2)

			const timestamp = today.toISOString()

			const result = convertTimestampToRelative(timestamp, undefined, true)

			expect(result).toStrictEqual('about 2 years ago')
		})
	})

	describe('convertMicroCcdToCcd', () => {
		it('should convert microCCD into CCD', () => {
			expect(convertMicroCcdToCcd(1337)).toStrictEqual('0.001337')
		})

		it('should return a fixed number of decimals', () => {
			expect(convertMicroCcdToCcd(1_337_000)).toStrictEqual('1.337000')
		})

		it('should default to 0 if no number is provided', () => {
			expect(convertMicroCcdToCcd(undefined)).toStrictEqual('0.000000')
		})
	})

	describe('formatNumber', () => {
		it('should format a number to use thousand seperators', () => {
			expect(formatNumber(1_337_666.42)).toStrictEqual('1,337,666.42')
		})
	})

	describe('calculateWeight', () => {
		it('should calculate the weight of an amount', () => {
			expect(calculateWeight(25, 500)).toStrictEqual('5.00')
		})

		it('should round the result to two decimals', () => {
			expect(calculateWeight(25, 600)).toStrictEqual('4.17')
		})
	})

	describe('shortenHash', () => {
		it('should shorten a long hash', () => {
			expect(shortenHash('b4da55abc123def456')).toStrictEqual('b4da55')
		})

		it('should have a fallback if there is no hash', () => {
			expect(shortenHash()).toStrictEqual('')
			expect(shortenHash(undefined)).toStrictEqual('')

			// @ts-expect-error : test for fallback
			expect(shortenHash(null)).toStrictEqual('')
		})
	})
})
