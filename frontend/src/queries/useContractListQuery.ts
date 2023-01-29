import { useQuery, gql } from '@urql/vue'
import type { Contract, PageInfo } from '~/types/generated'
import type { QueryVariables } from '~/types/queryVariables'

type ContractListResponse = {
	contracts: {
		nodes: Contract[]
		pageInfo: PageInfo
	}
}

const Query = gql<ContractListResponse>`
	query ($after: String, $before: String, $first: Int, $last: Int) {
		contracts(after: $after, before: $before, first: $first, last: $last) {
			nodes {
				id
				contractAddress {
					asString
				}
				owner {
					asString
				}
				transactionsCount
				createdTime
				balance
				moduleRef
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
type Variables = Partial<QueryVariables>

export const useContractsListQuery = (variables: Variables) => {
	const { data } = useQuery({
		query: Query,
		requestPolicy: 'cache-and-network',
		variables,
	})

	return { data }
}
