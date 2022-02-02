import { useQuery, gql } from '@urql/vue'
import type { Transaction } from '~/types/transactions'

type TransactionResponse = {
	transaction: Transaction
}
type TransactionByTransactionHashResponse = {
	transactionByTransactionHash: Transaction
}

const TransactionQuery = gql<TransactionResponse>`
	query ($id: ID!) {
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
					events {
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
	query ($hash: String!) {
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
					events {
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
export const useTransactionQueryByHash = (hash: string) => {
	const { data } = useQuery({
		query: TransactionQueryByHash,
		requestPolicy: 'cache-first',
		variables: {
			hash,
		},
	})

	return { data }
}

export const useTransactionQuery = (id: string) => {
	const { data } = useQuery({
		query: TransactionQuery,
		requestPolicy: 'cache-first',
		variables: {
			id,
		},
	})

	return { data }
}
