import { useQuery, gql } from '@urql/vue'
import type { Account } from '~/types/generated'
import type { QueryVariables } from '~/types/queryVariables'

type PageInfo = {
	hasNextPage: boolean
	hasPreviousPage: boolean
	startCursor?: string
	endCursor?: string
}

type AccountsListResponse = {
	accounts: {
		nodes: Account[]
		pageInfo: PageInfo
	}
}

const AccountsQuery = gql<AccountsListResponse>`
	query ($after: String, $before: String, $first: Int, $last: Int) {
		accounts(after: $after, before: $before, first: $first, last: $last) {
			nodes {
				id
				address
				createdAt
				transactions {
					nodes {
						transaction {
							transactionHash
							id
						}
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

export const useAccountsListQuery = (variables: Partial<QueryVariables>) => {
	const { data, executeQuery } = useQuery({
		query: AccountsQuery,
		requestPolicy: 'cache-and-network',
		variables,
	})

	return { data, executeQuery }
}
