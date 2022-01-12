import { convertTimestampToRelative, convertMicroCcdToCcd } from './format'

describe('format', () => {
	describe('convertTimestampToRelative', () => {
		it('should convert a timestamp into a relative time distance', () => {
			const timestamp = '2000-01-01T00:00:00.000Z'
			const compareDate = new Date(2000, 0, 11)

			const result = convertTimestampToRelative(timestamp, compareDate)

			expect(result).toStrictEqual('10 days ago')
		})

		it('should default the comparision date to current date', () => {
			const today = new Date()
			today.setFullYear(today.getFullYear() - 2)

			const timestamp = today.toISOString()

			const result = convertTimestampToRelative(timestamp)

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
})
