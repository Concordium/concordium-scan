import {
	calculateAmountAvailable,
	formatDelegationAvailableTooltip,
} from './stakingAndDelegation'

describe('stakingAndDelegation', () => {
	describe('calculateAmountAvailable', () => {
		it('should return the amount available if it is within bounds', () => {
			expect(calculateAmountAvailable(80, 120)).toBe(40)
		})

		it('should return 0 if cap is exceeded', () => {
			expect(calculateAmountAvailable(120, 105)).toBe(0)
		})
	})

	describe('formatDelegationAvailableTooltip', () => {
		it('should return a formatted string if amount is within bounds', () => {
			const result = formatDelegationAvailableTooltip(160, 200)

			expect(result).toBe('20.00% of cap available for delegation')
		})

		it('should return a formatted string if cao is exceeded', () => {
			const first = formatDelegationAvailableTooltip(200, 100)
			const second = formatDelegationAvailableTooltip(25, 20)

			expect(first).toBe('Delegation cap exceeded by 100.00%')
			expect(second).toBe('Delegation cap exceeded by 25.00%')
		})
	})
})
