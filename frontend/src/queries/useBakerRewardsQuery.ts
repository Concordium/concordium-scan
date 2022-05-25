import { useQuery, gql } from '@urql/vue'
import { useComponentState } from '~/composables/useComponentState'
import type { Baker, BakerReward } from '~/types/generated'
import type { QueryVariables } from '~/types/queryVariables'

type BakerRewardsResponse = {
	bakerByBakerId: Baker
}

const BakerRewardsQuery = gql<BakerRewardsResponse>`
	query (
		$bakerId: Long!
		$after: String
		$before: String
		$first: Int
		$last: Int
	) {
		bakerByBakerId(bakerId: $bakerId) {
			rewards(after: $after, before: $before, first: $first, last: $last) {
				nodes {
					block {
						blockHash
					}
					id
					timestamp
					rewardType
					amount
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
`

export const useBakerRewardsQuery = (
	bakerId: number,
	variables: Partial<QueryVariables>
) => {
	const { data, fetching, error } = useQuery({
		query: BakerRewardsQuery,
		requestPolicy: 'cache-and-network',
		variables: {
			bakerId,
			...variables,
		},
	})

	const dataRef = ref(data.value?.bakerByBakerId?.rewards?.nodes?.[0])

	const componentState = useComponentState<BakerReward | undefined>({
		fetching,
		error,
		data: dataRef,
	})

	watch(
		() => data.value,
		value => (dataRef.value = value?.bakerByBakerId?.rewards?.nodes?.[0])
	)

	return { data, error, componentState }
}
