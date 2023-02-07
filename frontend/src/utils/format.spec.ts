import {
	tillNextPayday,
	convertTimestampToRelative,
	convertMicroCcdToCcd,
	calculatePercentage,
	formatNumber,
	formatTimestamp,
	shortenHash,
	formatSeconds,
} from './format'

describe('format', () => {
	describe('formatTimestamp', () => {
		it('should format a date to locale', () => {
			const timestamp = '1969-07-20T20:17:40.000Z'

			// assuming en-US in UTC
			expect(formatTimestamp(timestamp)).toBe('Jul 20, 1969, 8:17 PM')
		})
	})

	describe('convertTimestampToRelative', () => {
		it('should convert a timestamp into a relative time distance', () => {
			const timestamp = '2000-01-01T00:00:00.000Z'
			const compareDate = new Date(2000, 0, 11)

			const result = convertTimestampToRelative(timestamp, compareDate)

			expect(result).toBe('10 days')
		})

		it('should default the comparision date to current date', () => {
			const today = new Date()
			today.setFullYear(today.getFullYear() + 2)
			// prevent flakyness caused by "almost" and "about" both being N years
			today.setMonth(today.getMonth() + 2)

			const timestamp = today.toISOString()

			const result = convertTimestampToRelative(timestamp)

			expect(result).toBe('about 2 years')
		})

		it('should show a suffix for a future date', () => {
			const today = new Date()
			today.setFullYear(today.getFullYear() + 2)
			// prevent flakyness caused by "almost" and "about" both being N years
			today.setMonth(today.getMonth() + 2)

			const timestamp = today.toISOString()

			const result = convertTimestampToRelative(timestamp, undefined, true)

			expect(result).toBe('in about 2 years')
		})

		it('should show a suffix for a past date', () => {
			const today = new Date()
			today.setFullYear(today.getFullYear() - 2)
			// prevent flakyness caused by "almost" and "about" both being N years
			today.setMonth(today.getMonth() - 2)

			const timestamp = today.toISOString()

			const result = convertTimestampToRelative(timestamp, undefined, true)

			expect(result).toBe('about 2 years ago')
		})
	})

	describe('tillNextPayday', () => {
		const msInHour = 3600000

		it('should return time till next pay day when effective time is > payday time', () => {
			const effectiveTime = '2022-12-11T19:33:26.500Z'
			const paydayTime = '2022-11-28T11:05:19.500Z'

			const result = tillNextPayday(effectiveTime, paydayTime, 24 * msInHour)
			expect(result).toBe('2022-12-12T11:05:19.500Z')
		})

		it('should return time till next pay day when effective time is > payday time & payday duration is != 24', () => {
			const effectiveTime = '2022-12-11T19:33:26.500Z'
			const paydayTime = '2022-11-28T11:05:19.500Z'

			const result = tillNextPayday(effectiveTime, paydayTime, 25 * msInHour)
			expect(result).toBe('2022-12-12T00:05:19.500Z')
		})

		it('should return time till next pay day when next payday time is on same date before payday time', () => {
			const effectiveTime = '2022-11-28T10:05:19.500Z'
			const paydayTime = '2022-11-28T11:05:19.500Z'

			const result = tillNextPayday(effectiveTime, paydayTime, 24 * msInHour)
			expect(result).toBe(paydayTime)
		})

		it('should return time till next pay day when payday is on same date after payday time', () => {
			const effectiveTime = '2022-11-28T12:05:19.500Z'
			const paydayTime = '2022-11-28T11:05:19.500Z'

			const result = tillNextPayday(effectiveTime, paydayTime, 24 * msInHour)
			expect(result).toBe('2022-11-29T11:05:19.500Z')
		})
	})

	describe('convertMicroCcdToCcd', () => {
		it('should convert microCCD into CCD', () => {
			expect(convertMicroCcdToCcd(1337)).toBe('0.001337')
		})

		it('should return a fixed number of decimals', () => {
			expect(convertMicroCcdToCcd(1_337_000)).toBe('1.337000')
		})

		it('should default to a dash if no integer is provided', () => {
			// micro CCD shouldn't be fractional
			expect(convertMicroCcdToCcd(1_337.42)).toBe('-')
			expect(convertMicroCcdToCcd(NaN)).toBe('-')
			// @ts-expect-error : test for fallback
			expect(convertMicroCcdToCcd(null)).toBe('-')
			// @ts-expect-error : test for fallback
			expect(convertMicroCcdToCcd(undefined)).toBe('-')
		})

		it('can convert a microCCD into CCD rounded to nearest full CCD', () => {
			expect(convertMicroCcdToCcd(1_337_000, true)).toBe('1')
			expect(convertMicroCcdToCcd(1_666_000, true)).toBe('2')
		})
	})

	describe('formatNumber', () => {
		it('should format a number to use thousand seperators', () => {
			expect(formatNumber(1_337_666.42)).toBe('1,337,666.42')
		})

		it('should default to a dash if no number is provided', () => {
			expect(formatNumber(NaN)).toBe('-')
			// @ts-expect-error : test for fallback
			expect(formatNumber(null)).toBe('-')
			// @ts-expect-error : test for fallback
			expect(formatNumber(undefined)).toBe('-')
		})

		it('should format the number 0', () => {
			expect(formatNumber(0)).toBe('0')
		})
	})

	describe('calculatePercentage', () => {
		it('should calculate the percentage of an amount', () => {
			expect(calculatePercentage(25, 500)).toBe(5)
		})
	})

	describe('shortenHash', () => {
		it('should shorten a long hash', () => {
			expect(shortenHash('b4da55abc123def456')).toBe('b4da55')
		})

		it('should have a fallback if there is no hash', () => {
			expect(shortenHash()).toBe('')
			expect(shortenHash(undefined)).toBe('')

			// @ts-expect-error : test for fallback
			expect(shortenHash(null)).toBe('')
		})
	})

	describe('formatSeconds', () => {
		it('should always format to exactly one decimal', () => {
			expect(formatSeconds(10)).toBe('10.0')
			expect(formatSeconds(10.01)).toBe('10.0')
			expect(formatSeconds(10.91)).toBe('10.9')
		})
	})
})
