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
	accountAddress
}
... on AmountAddedByDecryption {
	amount
	accountAddress
}
... on BakerAdded {
	bakerId
	accountAddress
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
			address
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
	accountAddress
	newCredIds
	removedCredIds
}
... on DataRegistered {
	__typename
}
...on EncryptedAmountsRemoved {
	accountAddress
}
... on EncryptedSelfAmountAdded {
	accountAddress
	amount
}
...on NewEncryptedAmount {
	accountAddress
}
... on TransferMemo {
	decoded {
		text
		decodeType
	}
}
... on TransferredWithSchedule {
	fromAccountAddress
	toAccountAddress
}
...on ChainUpdateEnqueued {
	__typename
}
... on Transferred {
	amount
	from {
		... on AccountAddress {
			__typename
			address
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
			address
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
	accountAddress
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
				address
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
		accountAddress
	}
	... on InvalidAccountReference {
		accountAddress
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
		accountAddress
	}
	... on NotABaker {
		accountAddress
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
		accountAddress
	}
}
`

const TransactionQuery = gql<TransactionResponse>`
	query ($id: ID!, $after: String, $before: String, $first: Int, $last: Int) {
		transaction(id: $id) {
			id
			ccdCost
			transactionHash
			senderAccountAddress
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
			senderAccountAddress
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
