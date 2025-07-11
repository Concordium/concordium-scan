import type { Ref } from 'vue'
import { useQuery, gql } from '@urql/vue'
import type { PageInfo, ContractEdge } from '../types/generated'
import type { QueryVariables } from '../types/queryVariables'

export type ContractListResponse = {
	contracts: {
		edges: ContractEdge[]
		pageInfo: PageInfo
	}
}

const Query = gql<ContractListResponse>`
	query ($after: String, $before: String, $first: Int, $last: Int) {
		contracts(after: $after, before: $before, first: $first, last: $last) {
			edges {
				node {
					snapshot {
						amount
						moduleReference
					}
					transactionHash
					contractAddress
					contractAddressIndex
					contractAddressSubIndex
					blockSlotTime
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
): { data: Ref<ContractListResponse | undefined> } => {
	const { data } = useQuery({
		query: Query,
		requestPolicy: 'cache-and-network',
		variables,
	})

	return { data }
}
