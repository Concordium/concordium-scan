import {
	calculateAmountAvailable,
	formatDelegationAvailableTooltip,
} from './stakingAndDelegation'

describe('stakingAndDelegation', () => {
	describe('calculateAmountAvailable', () => {
		it('should return the amount available if it is within bounds', () => {
			expect(calculateAmountAvailable(80, 120)).toBe(40)
		})

		it('should return 0 if cap is exceeded or met', () => {
			expect(calculateAmountAvailable(120, 105)).toBe(0)
			expect(calculateAmountAvailable(120, 120)).toBe(0)
			expect(calculateAmountAvailable(120, 0)).toBe(0)
			expect(calculateAmountAvailable(0, 0)).toBe(0)
		})
	})

	describe('formatDelegationAvailableTooltip', () => {
		it('should return a formatted string with fill percentage if amount is within bounds', () => {
			const result = formatDelegationAvailableTooltip(160, 200)

			expect(result).toBe('20.00% of cap available for delegation')
		})

		it('should return a formatted string with percentage if cap is exceeded', () => {
			const first = formatDelegationAvailableTooltip(200, 100)
			const second = formatDelegationAvailableTooltip(25, 20)

			expect(first).toBe('Delegation cap exceeded by 100.00%')
			expect(second).toBe('Delegation cap exceeded by 25.00%')
		})

		it('should return a simple string if cap is filled exactly', () => {
			const result = formatDelegationAvailableTooltip(20, 20)

			expect(result).toBe('Delegation cap filled exactly')
		})

		it("should return a warning when the baker's own stake exceeds capital bound", () => {
			const first = formatDelegationAvailableTooltip(0, 0)
			const second = formatDelegationAvailableTooltip(20, 0)

			expect(first).toBe('Baker stake exceeds capital bounds')
			expect(second).toBe('Baker stake exceeds capital bounds')
		})
	})
})
