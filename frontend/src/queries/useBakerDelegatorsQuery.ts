import { useQuery, gql } from '@urql/vue'
import { useComponentState } from '~/composables/useComponentState'
import type { Baker, DelegationSummary } from '~/types/generated'
import type { QueryVariables } from '~/types/queryVariables'

type BakerDelegatorsResponse = {
	bakerByBakerId: Baker
}

const BakerDelegatorQuery = gql<BakerDelegatorsResponse>`
	query (
		$bakerId: Long!
		$after: String
		$before: String
		$first: Int
		$last: Int
	) {
		bakerByBakerId(bakerId: $bakerId) {
			state {
				__typename
				... on ActiveBakerState {
					pool {
						totalStakePercentage
						delegatorCount
						delegators(
							after: $after
							before: $before
							first: $first
							last: $last
						) {
							nodes {
								stakedAmount
								restakeEarnings
								accountAddress {
									asString
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
				}
			}
		}
	}
`

export const useBakerDelegatorsQuery = (
	bakerId: number,
	variables: Partial<QueryVariables>
) => {
	const { data, fetching, error } = useQuery({
		query: BakerDelegatorQuery,
		requestPolicy: 'cache-and-network',
		variables: {
			bakerId,
			...variables,
		},
	})

	const dataRef = ref<DelegationSummary | undefined>(
		data.value?.bakerByBakerId?.state?.__typename === 'ActiveBakerState'
			? data.value.bakerByBakerId?.state?.pool?.delegators?.nodes?.[0]
			: undefined
	)

	const componentState = useComponentState<DelegationSummary | undefined>({
		fetching,
		error,
		data: dataRef,
	})

	watch(
		() => data.value,
		value => {
			dataRef.value =
				value?.bakerByBakerId?.state?.__typename === 'ActiveBakerState'
					? value.bakerByBakerId.state.pool?.delegators?.nodes?.[0]
					: undefined
		}
	)

	return { data, error, componentState }
}
