import { useQuery, gql } from '@urql/vue'
import { Ref } from 'vue'
import { useComponentState } from '~/composables/useComponentState'
import type { Transaction } from '~/types/generated'
import type { QueryVariables } from '~/types/queryVariables'

type TransactionResponse = {
	transaction: Transaction
}
type TransactionByTransactionHashResponse = {
	transactionByTransactionHash: Transaction
}

const eventsFragment = `
__typename
... on AccountCreated {
	accountAddress {
		asString
	}
}
... on AmountAddedByDecryption {
	amount
	accountAddress {
		asString
	}
}
... on BakerAdded {
	bakerId
	accountAddress {
		asString
	}
	stakedAmount
	restakeEarnings
}
... on BakerKeysUpdated {
	bakerId
}
... on BakerRemoved {
	bakerId
}
... on BakerSetRestakeEarnings {
	bakerId
	restakeEarnings
}
... on BakerStakeDecreased {
	bakerId
	newStakedAmount
}
... on BakerStakeIncreased {
	bakerId
	newStakedAmount
}
... on ContractInitialized {
	contractAddress {
		__typename
		index
		subIndex
	}
	amount
	moduleRef
}
... on ContractModuleDeployed {
	moduleRef
}
... on ContractUpdated {
	instigator {
		__typename
		... on AccountAddress {
			asString
		}
		... on ContractAddress {
			index
			subIndex
		}
	}
	contractAddress {
		__typename
		index
		subIndex
	}
}
... on CredentialKeysUpdated {
	credId
}
... on CredentialsUpdated {
	accountAddress {
		asString
	}
	newCredIds
	removedCredIds
}
... on DataRegistered {
	__typename
}
...on EncryptedAmountsRemoved {
	accountAddress {
		asString
	}
}
... on EncryptedSelfAmountAdded {
	accountAddress {
		asString
	}
	amount
}
...on NewEncryptedAmount {
	accountAddress {
		asString
	}
}
... on TransferMemo {
	decoded {
		text
		decodeType
	}
}
... on TransferredWithSchedule {
	fromAccountAddress {
		asString
	}
	toAccountAddress {
		asString
	}
	totalAmount
	amountsSchedule {
		pageInfo {
			hasNextPage
			hasPreviousPage
			startCursor
			endCursor
		}
		nodes {
			timestamp
			amount
		}
	}
}
...on ChainUpdateEnqueued {
	__typename
	effectiveTime
	payload {
		__typename
		...on AddAnonymityRevokerChainUpdatePayload {
			name
		}
		...on AddIdentityProviderChainUpdatePayload {
			name
		}
		...on BakerStakeThresholdChainUpdatePayload {
			amount
		}
		...on ElectionDifficultyChainUpdatePayload {
			electionDifficulty
		}
		...on EuroPerEnergyChainUpdatePayload {
			exchangeRate {
				numerator
				denominator
			}
		}
		...on FoundationAccountChainUpdatePayload {
			accountAddress {
				asString
			}
		}
		...on GasRewardsChainUpdatePayload {
			accountCreation
			baker
			chainUpdate
			finalizationProof
		}
		...on MicroCcdPerEuroChainUpdatePayload {
			exchangeRate {
				denominator
				numerator
			}
		}
		...on MintDistributionChainUpdatePayload {
			bakingReward
			finalizationReward
			mintPerSlot
		}
		...on ProtocolChainUpdatePayload {
			message
			specificationUrl
		}
		...on TransactionFeeDistributionChainUpdatePayload {
			baker
			gasAccount
		}
	}
}
... on Transferred {
	amount
	from {
		... on AccountAddress {
			__typename
			asString
		}
		... on ContractAddress {
			__typename
			index
			subIndex
		}
	}
	to {
		... on AccountAddress {
			__typename
			asString
		}
		... on ContractAddress {
			__typename
			index
			subIndex
		}
	}
}
... on CredentialDeployed {
	regId
	accountAddress {
		asString
	}
}
... on BakerSetOpenStatus {
	bakerId
	accountAddress {asString}
	openStatus
}
... on BakerSetMetadataURL {
	bakerId
	accountAddress {asString}
	metadataUrl
}
... on BakerSetTransactionFeeCommission {
	bakerId
	accountAddress {asString}
	transactionFeeCommission
}
... on BakerSetBakingRewardCommission {
	bakerId
	accountAddress {asString}
	bakingRewardCommission
}
... on BakerSetFinalizationRewardCommission {
	bakerId
	accountAddress {asString}
	finalizationRewardCommission
}
... on DelegationStakeIncreased {
	delegatorId
	accountAddress {asString}
	newStakedAmount
}
... on DelegationStakeDecreased {
	delegatorId
	accountAddress {asString}
	newStakedAmount
}
... on DelegationSetRestakeEarnings {
	delegatorId
	accountAddress {asString}
	restakeEarnings
}
... on DelegationSetDelegationTarget {
	delegatorId
	accountAddress {asString}
	delegationTarget {
		... on BakerDelegationTarget {
			bakerId
		}
		... on PassiveDelegationTarget {
			__typename
		}
		__typename}
}
... on DelegationAdded {
	delegatorId
	accountAddress {asString}
}
... on DelegationRemoved {
	delegatorId
	accountAddress {asString}
}
`

const rejectionFragment = `
reason {
	__typename,
	... on AlreadyABaker {
		bakerId
	}
	... on AmountTooLarge {
		amount
		address {
			__typename
			... on AccountAddress {
				asString
			}
			... on ContractAddress {
				index
				subIndex
			}
		}
	}
	... on DuplicateCredIds {
		credIds
	}
	... on EncryptedAmountSelfTransfer {
		accountAddress {
			asString
		}
	}
	... on InvalidAccountReference {
		accountAddress {
			asString
		}
	}
	... on InvalidContractAddress {
		contractAddress {
			index
			subIndex
		}
	}
	... on InvalidInitMethod {
		moduleRef
		initName
	}
	... on InvalidModuleReference {
		moduleRef
	}
	... on InvalidReceiveMethod {
		moduleRef
		receiveName
	}
	... on ModuleHashAlreadyExists {
		moduleRef
	}
	... on NonExistentCredIds {
		credIds
	}
	... on NonExistentRewardAccount {
		accountAddress {
			asString
		}
	}
	... on NotABaker {
		accountAddress {
			asString
		}
	}
	... on RejectedInit {
		rejectReason
	}
	... on RejectedReceive {
		rejectReason
		contractAddress {
			index
			subIndex
		}
		receiveName
	}
	... on ScheduledSelfTransfer {
		accountAddress {
			asString
		}
	}
  ... on MissingBakerAddParameters {
	  __typename
  }
  ... on FinalizationRewardCommissionNotInRange {
		__typename
  }
  ... on BakingRewardCommissionNotInRange {
		__typename
  }
  ... on TransactionFeeCommissionNotInRange {
		__typename
  }
  ... on AlreadyADelegator {
		__typename
  }
  ... on InsufficientBalanceForDelegationStake {
		__typename
  }
  ... on MissingDelegationAddParameters {
		__typename
  }
  ... on InsufficientDelegationStake {
		__typename

  }
  ... on DelegatorInCooldown {
		__typename
  }
  ... on NotADelegator {
		accountAddress {asString}
		__typename
  }
  ... on DelegationTargetNotABaker {
		bakerId
		__typename
  }
  ... on StakeOverMaximumThresholdForPool {
		__typename
  }
  ... on PoolWouldBecomeOverDelegated {
		__typename
  }
  ... on PoolClosed {
		__typename
  }
}
`

const TransactionQuery = gql<TransactionResponse>`
	query ($id: ID!, $after: String, $before: String, $first: Int, $last: Int) {
		transaction(id: $id) {
			id
			ccdCost
			transactionHash
			senderAccountAddress {
				asString
			}
			block {
				id
				blockHash
				blockHeight
				blockSlotTime
			}
			result {
				... on Success {
					events(after: $after, before: $before, first: $first, last: $last) {
						nodes { ${eventsFragment} }
						totalCount
						pageInfo {
							startCursor
							endCursor
							hasPreviousPage
							hasNextPage
						}
					}
				}
				... on Rejected { ${rejectionFragment} }
			}
			transactionType {
				__typename
				... on AccountTransaction {
					accountTransactionType
				}
				... on CredentialDeploymentTransaction {
					credentialDeploymentTransactionType
				}
				... on UpdateTransaction {
					updateTransactionType
				}
			}
		}
	}
`

const TransactionQueryByHash = gql<TransactionByTransactionHashResponse>`
	query (
		$hash: String!
		$after: String
		$before: String
		$first: Int
		$last: Int
	) {
		transactionByTransactionHash(transactionHash: $hash) {
			id
			ccdCost
			transactionHash
			senderAccountAddress {
				asString
			}
			block {
				id
				blockHash
				blockHeight
				blockSlotTime
			}
			result {
				... on Success {
					events(after: $after, before: $before, first: $first, last: $last) {
						nodes { ${eventsFragment} }
						totalCount
						pageInfo {
							startCursor
							endCursor
							hasPreviousPage
							hasNextPage
						}
					}
				}
				... on Rejected { ${rejectionFragment} }
			}
			transactionType {
				__typename
				... on AccountTransaction {
					accountTransactionType
				}
				... on CredentialDeploymentTransaction {
					credentialDeploymentTransactionType
				}
				... on UpdateTransaction {
					updateTransactionType
				}
			}
		}
	}
`
type QueryParams = (
	| {
			id: Ref<string>
			hash?: Ref<string>
	  }
	| {
			hash: Ref<string>
			id?: Ref<string>
	  }
) & {
	eventsVariables?: QueryVariables
}

const getData = (
	responseData:
		| TransactionResponse
		| TransactionByTransactionHashResponse
		| undefined
): Transaction | undefined => {
	if (!responseData) return undefined

	return 'transaction' in responseData
		? responseData.transaction
		: responseData.transactionByTransactionHash
}

export const useTransactionQuery = ({
	id,
	hash,
	eventsVariables,
}: QueryParams) => {
	const query = id?.value ? TransactionQuery : TransactionQueryByHash
	const identifier = id?.value ? { id: id.value } : { hash: hash?.value }

	const { data, fetching, error } = useQuery({
		query,
		requestPolicy: 'cache-first',
		variables: {
			...identifier,
			...eventsVariables,
		},
	})

	const dataRef = ref(getData(data.value))

	const componentState = useComponentState<Transaction | undefined>({
		fetching,
		error,
		data: dataRef,
	})

	watch(
		() => data.value,
		value => (dataRef.value = getData(value))
	)

	return { data: dataRef, error, componentState }
}
