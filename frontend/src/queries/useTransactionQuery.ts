import { useQuery, gql } from '@urql/vue'
import type { Transaction } from '~/types/transactions'
import type { QueryVariables } from '~/types/queryVariables'

type TransactionResponse = {
	transaction: Transaction
}
type TransactionByTransactionHashResponse = {
	transactionByTransactionHash: Transaction
}

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
				successful
				... on Successful {
					events(after: $after, before: $before, first: $first, last: $last) {
						nodes {
							__typename
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
							... on AccountCreated {
								address
							}
							... on CredentialDeployed {
								regId
								accountAddress
							}
						}
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
				successful
				... on Successful {
					events(after: $after, before: $before, first: $first, last: $last) {
						nodes {
							__typename
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
							... on AccountCreated {
								address
							}
							... on CredentialDeployed {
								regId
								accountAddress
							}
						}
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
