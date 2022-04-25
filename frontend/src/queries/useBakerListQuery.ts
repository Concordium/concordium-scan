import { useQuery, gql } from '@urql/vue'
import type { Baker, PageInfo } from '~/types/generated'
import type { QueryVariables } from '~/types/queryVariables'

type BakerListResponse = {
	bakers: {
		nodes: Baker[]
		pageInfo: PageInfo
	}
}

const BakerQuery = gql<BakerListResponse>`
	query ($after: String, $before: String, $first: Int, $last: Int) {
		bakers(after: $after, before: $before, first: $first, last: $last) {
			nodes {
				bakerId
				account {
					id
					address {
						asString
					}
				}
				state {
					... on ActiveBakerState {
						__typename
						stakedAmount
					}
					... on RemovedBakerState {
						__typename
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

export const useBakerListQuery = (variables: Partial<QueryVariables>) => {
	const { data } = useQuery({
		query: BakerQuery,
		requestPolicy: 'cache-first',
		variables,
	})

	return { data }
}
