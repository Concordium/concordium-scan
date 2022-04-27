import { translateAccountStatementEntryType } from './translateAccountStatementEntry'
import type { AccountStatementEntryType } from '~/types/generated'

describe('translateAccountStatementEntryType', () => {
	it('should have a fallback for missing entry types', () => {
		// @ts-expect-error : test for fallback
		expect(translateAccountStatementEntryType(undefined)).toBe('Unknown')

		expect(
			// @ts-expect-error : test for fallback
			translateAccountStatementEntryType('BendingUnitPleaseInsertLiquor')
		).toBe('Unknown')
	})

	it('should translate known entry types', () => {
		expect(
			translateAccountStatementEntryType(
				'MINT_REWARD' as AccountStatementEntryType
			)
		).toBe('Reward')
		expect(
			translateAccountStatementEntryType(
				'AMOUNT_ENCRYPTED' as AccountStatementEntryType
			)
		).toBe('Encrypted')
	})
})
