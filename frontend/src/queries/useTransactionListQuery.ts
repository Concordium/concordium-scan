import { useQuery, gql } from '@urql/vue'
import type { Transaction } from '~/types/generated'
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
				senderAccountAddress {
					asString
				}
				block {
					blockHeight
					blockSlotTime
				}
				result {
					__typename
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

export const useTransactionsListQuery = (
	variables: Partial<QueryVariables>
) => {
	const { data, executeQuery } = useQuery({
		context: {
			url: inject<string>('fungyApiUrl'),
		},
		query: TransactionsQuery,
		requestPolicy: 'cache-and-network',
		variables,
	})

	return { data, executeQuery }
}
