import { Ref } from 'vue'
import { useQuery, gql } from '@urql/vue'
import type { Baker, BakerSort, PageInfo } from '~/types/generated'
import type { QueryVariables } from '~/types/queryVariables'

type BakerListResponse = {
	bakers: {
		nodes: Baker[]
		pageInfo: PageInfo
	}
}

type BakerListVariables = Partial<QueryVariables> & {
	sort: Ref<BakerSort>
}

const BakerQuery = gql<BakerListResponse>`
	query (
		$after: String
		$before: String
		$first: Int
		$last: Int
		$sort: BakerSort
	) {
		bakers(
			after: $after
			before: $before
			first: $first
			last: $last
			sort: $sort
		) {
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
						pool {
							openStatus
							totalStake
							delegatorCount
							delegatedStake
							delegatedStakeCap
							apy(period: LAST7_DAYS) {
								bakerApy
								delegatorsApy
								totalApy
							}
						}
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

export const useBakerListQuery = (variables: BakerListVariables) => {
	const { data } = useQuery({
		query: BakerQuery,
		requestPolicy: 'cache-first',
		variables,
	})

	return { data }
}
