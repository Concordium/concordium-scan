import type { TransactionRejectReason } from '~/types/generated'

// Non-exhaustive map of rejection reasons (excluding undefined) to translation strings
type TranslationMap = Partial<
	Record<NonNullable<TransactionRejectReason['__typename']>, string>
>

const translations: TranslationMap = {
	ModuleNotWf: 'Error raised when validating WASM module',
	RuntimeFailure: 'Runtime failure',
	SerializationFailure: 'Serialization failure',
	OutOfEnergy: 'Not enough energy to process this transaction',
	InvalidProof: 'Proof that the baker owns relevant private keys is not valid',
	InsufficientBalanceForBakerStake:
		'The account has insufficient funds to cover the proposed stake',
	StakeUnderMinimumThresholdForBaking:
		'The amount provided is below the threshold required for becoming a baker',
	BakerInCooldown:
		'The change could not be made because the baker is in cooldown for another change',
	DuplicateAggregationKey:
		'A baker with the given aggregation key already exists',
	NonExistentCredentialId: 'Credential ID does not exist',
	KeyIndexAlreadyInUse:
		'Attempted to add an account key to a key index already in use',
	InvalidAccountThreshold:
		'When the account threshold is updated, it must not exceed the amount of existing keys',
	InvalidCredentialKeySignThreshold:
		'When the credential key threshold is updated, it must not exceed the amount of existing keys',
	InvalidEncryptedAmountTransferProof:
		'Proof for an encrypted amount transfer did not validate',
	InvalidTransferToPublicProof:
		'Proof for a secret to public transfer did not validate',
	InvalidIndexOnEncryptedTransfer: 'Invalid index on encrypted transfer',
	ZeroScheduledAmount: 'A scheduled transfer can not have 0 amount',
	NonIncreasingSchedule:
		'The scheduled transfer does not have a strictly increasing schedule',
	FirstScheduledReleaseExpired:
		'The first scheduled release in a transfer with schedule has already expired',
	InvalidCredentials:
		'At least one of the credentials was either malformed or its proof was incorrect',
	RemoveFirstCredential: 'Attempt to remove the first crendential',
	CredentialHolderDidNotSign: 'The credential holder did not sign',
	NotAllowedMultipleCredentials:
		'Account is not allowed to have multiple credentials because it contains a non-zero encrypted transfer',
	NotAllowedToReceiveEncrypted:
		'The account is not allowed to receive encrypted transfers because it has multiple credentials',
	NotAllowedToHandleEncrypted:
		'The account is not allowed incoming or outgoing encrypted transfers',
	NonExistentTokenId: 'Token ID does not exist',
	TokenModule: 'Token module transaction reject reason',
	UnauthorizedTokenGovernance: 'Unauthorized token governance action',
}

export const translateRejectionReasons = (
	rejectReason: TransactionRejectReason
) => {
	return rejectReason.__typename && translations[rejectReason.__typename]
		? translations[rejectReason.__typename]
		: 'Unknown rejection reason'
}
