import { useQuery, gql } from '@urql/vue'
import type { Transaction } from '~/types/transactions'
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
	hash: string,
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
	id: string,
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
