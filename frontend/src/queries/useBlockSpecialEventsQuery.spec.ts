import { hasData } from './useBlockSpecialEventsQuery'

const pageInfo = {
	hasNextPage: false,
	hasPreviousPage: false,
}

describe('useBlockSpecialEventsQuery', () => {
	describe('hasData', () => {
		it('will be false if there is no data at all', () => {
			expect(hasData({})).toBe(false)
		})

		it('will be false if the data is empty', () => {
			const data = {
				bakingRewards: {
					nodes: [],
					pageInfo,
				},
				blockRewards: {
					nodes: [],
					pageInfo,
				},
			}

			expect(hasData(data)).toBe(false)
		})

		it('will be true if there is data to show', () => {
			const data = {
				bakingRewards: {
					nodes: ['some data'],
					pageInfo,
				},
				blockRewards: {
					nodes: [],
					pageInfo,
				},
			}

			expect(hasData(data)).toBe(true)
		})
	})
})
