import { convertMicroCcdToCcd, shortenHash } from './format'
import type { Event, Address } from '~/types/generated'

export const translateAddress = (address: Address) => {
	if (address.__typename === 'AccountAddress') {
		return `account ${shortenHash(address.address)}`
	} else if (address.__typename === 'ContractAddress') {
		return `contract <${address.index}, ${address.subIndex}>`
	}

	// This should never happen, but TS seems not to understand ternaries ...
	return 'an unknown address'
}

export const translateTransactionEvents = (txEvent: Event) => {
	if (txEvent.__typename === 'AccountCreated') {
		return `Account created with address ${shortenHash(txEvent.address)}`
	}

	if (txEvent.__typename === 'CredentialDeployed') {
		return `Deployed account with address ${shortenHash(
			txEvent.accountAddress
		)} from ${shortenHash(txEvent.regId)}`
	}

	if (txEvent.__typename === 'Transferred') {
		return `Transferred ${convertMicroCcdToCcd(
			txEvent.amount
		)}Ͼ from ${translateAddress(txEvent.from)} to ${translateAddress(
			txEvent.to
		)}`
	}

	return `Transaction event: ${txEvent.__typename}`
}
