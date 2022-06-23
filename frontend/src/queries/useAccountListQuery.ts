import type { Ref } from 'vue'
import { useQuery, gql } from '@urql/vue'
import type { Account, AccountSort, PageInfo } from '~/types/generated'
import type { QueryVariables } from '~/types/queryVariables'

type AccountsListResponse = {
	accounts: {
		nodes: Account[]
		pageInfo: PageInfo
	}
}

type AccountListVariables = Partial<QueryVariables> & {
	sort: Ref<AccountSort>
}

const AccountsQuery = gql<AccountsListResponse>`
	query (
		$after: String
		$before: String
		$first: Int
		$last: Int
		$sort: AccountSort
	) {
		accounts(
			after: $after
			before: $before
			first: $first
			last: $last
			sort: $sort
		) {
			nodes {
				id
				transactionCount
				address {
					asString
				}
				createdAt
				amount
				transactions {
					nodes {
						transaction {
							transactionHash
							block {
								blockSlotTime
							}
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

export const useAccountsListQuery = (variables: AccountListVariables) => {
	const { data } = useQuery({
		query: AccountsQuery,
		requestPolicy: 'cache-and-network',
		variables,
	})

	return { data }
}
