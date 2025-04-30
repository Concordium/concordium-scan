import type { Ref } from 'vue'
import { useQuery, gql } from '@urql/vue'
import type {
	Baker,
	BakerSort,
	BakerPoolOpenStatus,
	PageInfo,
} from '~/types/generated'
import type { QueryVariables } from '~/types/queryVariables'

type BakerListResponse = {
	bakers: {
		nodes: Baker[]
		pageInfo: PageInfo
	}
}

type BakerListVariables = Partial<QueryVariables> & {
	sort: Ref<BakerSort>
	filter: {
		openStatusFilter: Ref<BakerPoolOpenStatus | undefined>
		includeRemoved: Ref<boolean | undefined>
	}
}

const BakerQuery = gql<BakerListResponse>`
	query (
		$after: String
		$before: String
		$first: Int
		$last: Int
		$sort: BakerSort
		$filter: BakerFilterInput
	) {
		bakers(
			after: $after
			before: $before
			first: $first
			last: $last
			sort: $sort
			filter: $filter
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
							selfSuspended
							inactiveSuspended
							primedForSuspension
							delegatorCount
							delegatedStake
							delegatedStakeCap
							apy(period: LAST30_DAYS) {
								bakerApy
								delegatorsApy
							}
							commissionRates {
								transactionCommission
								bakingCommission
							}
							paydayCommissionRates {
								transactionCommission
								bakingCommission
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
		requestPolicy: 'cache-and-network',
		variables,
	})

	return { data }
}
