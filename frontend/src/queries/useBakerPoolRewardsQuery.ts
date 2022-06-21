import { useQuery, gql } from '@urql/vue'
import { useComponentState } from '~/composables/useComponentState'
import type {
	ActiveBakerState,
	Baker,
	PaydayPoolReward,
} from '~/types/generated'
import type { QueryVariables } from '~/types/queryVariables'

type BakerPoolRewardsResponse = {
	bakerByBakerId: Baker
}

const BakerPoolRewardsQuery = gql<BakerPoolRewardsResponse>`
	query (
		$bakerId: Long!
		$after: String
		$before: String
		$first: Int
		$last: Int
	) {
		bakerByBakerId(bakerId: $bakerId) {
			state {
				... on ActiveBakerState {
					stakedAmount
					restakeEarnings
					pool {
						poolRewards(
							after: $after
							before: $before
							first: $first
							last: $last
						) {
							nodes {
								block {
									blockHash
								}
								id
								timestamp
								bakerReward {
									bakerAmount
									delegatorsAmount
									totalAmount
								}
								finalizationReward {
									bakerAmount
									delegatorsAmount
									totalAmount
								}
								transactionFees {
									bakerAmount
									delegatorsAmount
									totalAmount
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
					__typename
				}
			}
		}
	}
`

export const useBakerPoolRewardsQuery = (
	bakerId: Baker['bakerId'],
	variables: Partial<QueryVariables>
) => {
	const { data, fetching, error } = useQuery({
		query: BakerPoolRewardsQuery,
		requestPolicy: 'cache-and-network',
		variables: {
			bakerId,
			...variables,
		},
	})
	const dataRef = ref(
		(data.value?.bakerByBakerId?.state as ActiveBakerState)?.pool?.poolRewards
			?.nodes?.[0]
	)

	const componentState = useComponentState<PaydayPoolReward | undefined>({
		fetching,
		error,
		data: dataRef,
	})

	watch(
		() => data.value,
		value =>
			(dataRef.value = (
				value?.bakerByBakerId?.state as ActiveBakerState
			)?.pool?.poolRewards?.nodes?.[0])
	)

	return { data, error, componentState }
}
