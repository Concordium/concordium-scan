import { translateRejectionReasons } from './translateRejectionReasons'
import type { TransactionRejectReason } from '~/types/generated'

describe('translateRejectionReasons', () => {
	it('should have a fallback for missing rejection reasons', () => {
		const undefinedRejection = {
			__typename: undefined,
		}

		const incorrectRejection = {
			__typename: 'BendingUnitPleaseInsertLiquor',
		}

		// @ts-expect-error : test for fallback
		expect(translateRejectionReasons(undefinedRejection)).toBe(
			'Unknown rejection reason'
		)

		// @ts-expect-error : test for fallback
		expect(translateRejectionReasons(incorrectRejection)).toBe(
			'Unknown rejection reason'
		)
	})

	it('should translate known rejection reasons', () => {
		const transactionType = {
			__typename: 'RuntimeFailure',
		} as TransactionRejectReason

		expect(translateRejectionReasons(transactionType)).toBe('Runtime failure')
	})
})
