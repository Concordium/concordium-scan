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
	accountAddressString
}
... on AmountAddedByDecryption {
	amount
	accountAddressString
}
... on BakerAdded {
	bakerId
	accountAddressString
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
	accountAddressString
	newCredIds
	removedCredIds
}
... on DataRegistered {
	__typename
}
...on EncryptedAmountsRemoved {
	accountAddressString
}
... on EncryptedSelfAmountAdded {
	accountAddressString
	amount
}
...on NewEncryptedAmount {
	accountAddressString
}
... on TransferMemo {
	decoded {
		text
		decodeType
	}
}
... on TransferredWithSchedule {
	fromAccountAddressString
	toAccountAddressString
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
	accountAddressString
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
		accountAddressString
	}
	... on InvalidAccountReference {
		accountAddressString
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
		accountAddressString
	}
	... on NotABaker {
		accountAddressString
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
		accountAddressString
	}
}
`

const TransactionQuery = gql<TransactionResponse>`
	query ($id: ID!, $after: String, $before: String, $first: Int, $last: Int) {
		transaction(id: $id) {
			id
			ccdCost
			transactionHash
			senderAccountAddressString
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
			senderAccountAddressString
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
