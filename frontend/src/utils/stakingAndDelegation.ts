import { formatPercentage } from '~/utils/format'

/**
 * Calculates amount available, with a lower boundary
 * @param {number} amount - Amount
 * @param {number} total - Total cap
 * @returns {string} - Amount available (0 if negative)
 * @example
 * // returns 5.00
 * calculateAmountAvailable(95, 100);
 * // returns 0
 * calculateAmountAvailable(110, 100);
 */
export const calculateAmountAvailable = (amount: number, cap: number) =>
	amount < cap ? cap - amount : 0

/**
 * Retuns tooltip text depending on cap filled, exceeded or met
 * @param {number} amount - Amount
 * @param {number} total - Total cap
 * @returns {string} - Formatted text
 * @example
 * formatDelegationAvailableTooltip(80, 100);
 */
export const formatDelegationAvailableTooltip = (
	amount: number,
	cap: number
) => {
	if (cap === 0) return 'Baker stake exceeds capital bounds'

	if (amount === cap) return 'Delegation cap filled exactly'

	return amount < cap
		? `${formatPercentage(
				(cap - amount) / cap
		  )}% of cap available for delegation`
		: `Delegation cap exceeded by ${formatPercentage((amount - cap) / cap)}%`
}
