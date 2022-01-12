import {
	type AccountTransaction,
	type UpdateTransaction,
	type CredentialDeploymentTransaction,
} from '../types/transactions'
import { translateTransactionType } from './translateTransactionTypes'

describe('translateTransactionTypes', () => {
	it('should have a fallback for unknown transaction types', () => {
		const transactionType = {
			__typename: 'KittenTransaction',
			kittenTransactionType: 'CUTE_KITTEN',
		}

		// @ts-expect-error : test for fallback
		expect(translateTransactionType(transactionType)).toStrictEqual(
			'Unknown transaction type'
		)
	})

	describe('account transactions', () => {
		it('should translate an account transaction', () => {
			const transactionType: AccountTransaction = {
				__typename: 'AccountTransaction',
				accountTransactionType: 'SIMPLE_TRANSFER',
			}

			expect(translateTransactionType(transactionType)).toStrictEqual(
				'Simple transfer'
			)
		})

		it('should have a fallback for an unknown account transaction', () => {
			const transactionType: AccountTransaction = {
				__typename: 'AccountTransaction',
				// @ts-expect-error : test for fallback
				accountTransactionType: 'KITTEN_TRANSFER',
			}

			expect(translateTransactionType(transactionType)).toStrictEqual(
				'Unknown account transaction'
			)
		})
	})

	describe('update transactions', () => {
		it('should translate an update transaction', () => {
			const transactionType: UpdateTransaction = {
				__typename: 'UpdateTransaction',
				updateTransactionType: 'UPDATE_GAS_REWARDS',
			}

			expect(translateTransactionType(transactionType)).toStrictEqual(
				'Update gas rewards'
			)
		})

		it('should have a fallback for an unknown update transaction', () => {
			const transactionType: UpdateTransaction = {
				__typename: 'UpdateTransaction',
				// @ts-expect-error : test for fallback
				updateTransactionType: 'UPDATE_KITTEN',
			}

			expect(translateTransactionType(transactionType)).toStrictEqual(
				'Unknown update transaction'
			)
		})
	})

	describe('credential deployment transactions', () => {
		it('should translate a credential deployment  transaction', () => {
			const transactionType: CredentialDeploymentTransaction = {
				__typename: 'CredentialDeploymentTransaction',
				credentialDeploymentTransactionType: 'INITIAL',
			}

			expect(translateTransactionType(transactionType)).toStrictEqual(
				'Initial credential deployment'
			)
		})

		it('should have a fallback for an unknown credential deployment transaction', () => {
			const transactionType: CredentialDeploymentTransaction = {
				__typename: 'CredentialDeploymentTransaction',
				// @ts-expect-error : test for fallback
				credentialDeploymentTransactionType: 'KITTEN_DEPLOYMENT',
			}

			expect(translateTransactionType(transactionType)).toStrictEqual(
				'Unknown credential deployment'
			)
		})
	})
})
