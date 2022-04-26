import { useQuery, gql } from '@urql/vue'
import { Ref } from 'vue'
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
export const useTransactionQueryByHash = (
	hash: Ref<string>,
	eventsVariables?: QueryVariables
) => {
	const { data } = useQuery({
		query: TransactionQueryByHash,
		requestPolicy: 'cache-first',
		variables: {
			hash,
			...eventsVariables,
		},
	})

	return { data }
}

export const useTransactionQuery = (
	id: Ref<string>,
	eventsVariables?: QueryVariables
) => {
	const { data } = useQuery({
		query: TransactionQuery,
		requestPolicy: 'cache-first',
		variables: {
			id,
			...eventsVariables,
		},
	})

	return { data }
}
