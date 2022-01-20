import {
	translateTransactionEvents,
	translateTransferAddress,
} from './translateTransactionEvents'
import type {
	TransactionSuccessfulEvent,
	TransferAddress,
} from '~/types/transactions'

describe('translateTransactionEvents', () => {
	it('should have a fallback translation for unknown events', () => {
		const txEvent = {
			__typename: 'KittenDeployment',
		}

		// @ts-expect-error : test for fallback
		expect(translateTransactionEvents(txEvent)).toBe(
			'Transaction event: KittenDeployment'
		)
	})

	it('should have a translation for account creation', () => {
		const txEvent = {
			__typename: 'AccountCreated',
			address: '1337address',
		} as TransactionSuccessfulEvent

		expect(translateTransactionEvents(txEvent)).toBe(
			'Account created with address 1337ad'
		)
	})

	it('should have a translation for credential deployment', () => {
		const txEvent = {
			__typename: 'CredentialDeployed',
			accountAddress: '1337address',
			regId: 'regid421337',
		} as TransactionSuccessfulEvent

		expect(translateTransactionEvents(txEvent)).toBe(
			'Deployed account with address 1337ad from regid4'
		)
	})

	it('should have a translation for a transfer', () => {
		const txEvent = {
			__typename: 'Transferred',
			from: { __typename: 'AccountAddress', address: 'sender123' },
			to: { __typename: 'AccountAddress', address: 'recipient' },
		} as TransactionSuccessfulEvent

		expect(translateTransactionEvents(txEvent)).toBe(
			'Transferred from account sender to account recipi'
		)
	})

	describe('translateTransferAddress', () => {
		it('should format an account address', () => {
			const address = {
				__typename: 'AccountAddress',
				address: 'accadd123',
			} as TransferAddress

			expect(translateTransferAddress(address)).toBe('account accadd')
		})

		it('should format a contract address', () => {
			const address = {
				__typename: 'ContractAddress',
				index: 'conindex42',
			} as TransferAddress

			expect(translateTransferAddress(address)).toBe('contract conind')
		})
	})
})
