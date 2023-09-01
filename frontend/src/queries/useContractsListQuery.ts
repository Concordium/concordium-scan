import { Ref } from 'vue'
import { useQuery, gql } from '@urql/vue'
import type { PageInfo, SmartContractsEdge } from '../types/generated'
import type { QueryVariables } from '../types/queryVariables'

export type SmartContractListResponse = {
	smartContracts: {
		edges: SmartContractsEdge[]
		pageInfo: PageInfo
	}
}

const Query = gql<SmartContractListResponse>`
	query ($after: String, $before: String, $first: Int, $last: Int) {
		smartContracts(after: $after, before: $before, first: $first, last: $last) {
			edges {
				node {
					amount
					transactionHash
					contractAddress {
						__typename
						asString
					}
					creator {
						__typename
						asString
					}
				}
			}
			pageInfo {
				startCursor
				endCursor
				hasNextPage
				hasPreviousPage
			}
		}
	}
`

type Variables = Partial<QueryVariables>

export const useContractsListQuery = (
	variables: Variables
): { data: Ref<SmartContractListResponse | undefined> } => {
	const { data } = useQuery({
		query: Query,
		requestPolicy: 'cache-and-network',
		variables,
	})

	return { data }
}
