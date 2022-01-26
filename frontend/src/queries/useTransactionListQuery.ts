import { useQuery, gql } from '@urql/vue'
import type { Transaction } from '~/types/transactions'
import type { QueryVariables } from '~/types/queryVariables'

type PageInfo = {
	hasNextPage: boolean
	hasPreviousPage: boolean
	startCursor?: string
	endCursor?: string
}

type TransactionListResponse = {
	transactions: {
		nodes: Transaction[]
		pageInfo: PageInfo
	}
}

const TransactionsQuery = gql<TransactionListResponse>`
	query ($after: String, $before: String, $first: Int, $last: Int) {
		transactions(after: $after, before: $before, first: $first, last: $last) {
			nodes {
				id
				ccdCost
				transactionHash
				senderAccountAddress
				block {
					blockHeight
					blockSlotTime
				}
				result {
					successful
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
			pageInfo {
				startCursor
				endCursor
				hasPreviousPage
				hasNextPage
			}
		}
	}
`

export const useTransactionsListQuery = (variables: QueryVariables) => {
	const { data, executeQuery } = useQuery({
		query: TransactionsQuery,
		requestPolicy: 'cache-and-network',
		variables,
	})

	return { data, executeQuery }
}
