import type {
	TransactionSuccessfulEvent,
	TransferAddress,
} from '~/types/transactions'

export const translateTransferAddress = (address: TransferAddress) =>
	address.__typename === 'AccountAddress'
		? `account ${address.address.substring(0, 6)}`
		: `contract ${address.index.substring(0, 6)}`

export const translateTransactionEvents = (
	txEvent: TransactionSuccessfulEvent
) => {
	if (txEvent.__typename === 'AccountCreated') {
		return `Account created with address ${txEvent.address.substring(0, 6)}`
	}

	if (txEvent.__typename === 'CredentialDeployed') {
		return `Deployed account with address ${txEvent.accountAddress.substring(
			0,
			6
		)} from ${txEvent.regId.substring(0, 6)}`
	}

	if (txEvent.__typename === 'Transferred') {
		return `Transferred from ${translateTransferAddress(
			txEvent.from
		)} to ${translateTransferAddress(txEvent.to)}`
	}

	// @ts-expect-error : fallback for unknwown events
	return `Transaction event: ${txEvent.__typename}`
}
