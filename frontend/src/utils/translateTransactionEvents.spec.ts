import {
	translateTransactionEvents,
	translateAddress,
} from './translateTransactionEvents'
import type { Event, Address } from '~/types/generated'

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
		} as Event

		expect(translateTransactionEvents(txEvent)).toBe(
			'Account created with address 1337ad'
		)
	})

	it('should have a translation for credential deployment', () => {
		const txEvent = {
			__typename: 'CredentialDeployed',
			accountAddress: '1337address',
			regId: 'regid421337',
		} as Event

		expect(translateTransactionEvents(txEvent)).toBe(
			'Deployed account with address 1337ad from regid4'
		)
	})

	it('should have a translation for a transfer', () => {
		const txEvent = {
			__typename: 'Transferred',
			amount: 1337042,
			from: { __typename: 'AccountAddress', address: 'sender123' },
			to: { __typename: 'AccountAddress', address: 'recipient' },
		} as Event

		expect(translateTransactionEvents(txEvent)).toBe(
			'Transferred 1.337042Ͼ from account sender to account recipi'
		)
	})

	describe('translateAddress', () => {
		it('should format an account address', () => {
			const address = {
				__typename: 'AccountAddress',
				address: 'accadd123',
			} as Address

			expect(translateAddress(address)).toBe('account accadd')
		})

		it('should format a contract address', () => {
			const address = {
				__typename: 'ContractAddress',
				index: 186,
				subIndex: 42,
			} as Address

			expect(translateAddress(address)).toBe('contract <186, 42>')
		})

		it('should have a fallback', () => {
			const address = {
				__typename: 'Unknown',
			} as unknown

			// @ts-expect-error : test for fallback
			expect(translateAddress(address)).toBe('an unknown address')
		})
	})
})
