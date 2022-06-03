import { useQuery, gql } from '@urql/vue'
import { useComponentState } from '~/composables/useComponentState'
import type { Account, AccountReward, BakerReward } from '~/types/generated'
import type { QueryVariables } from '~/types/queryVariables'

type BakerRewardsResponse = {
	accountByAddress: Account
}

const BakerRewardsQuery = gql<BakerRewardsResponse>`
	query (
		$accountAddress: String!
		$after: String
		$before: String
		$first: Int
		$last: Int
	) {
		accountByAddress(accountAddress: $accountAddress) {
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
	accountAddress: string,
	variables: Partial<QueryVariables>
) => {
	const { data, fetching, error } = useQuery({
		query: BakerRewardsQuery,
		requestPolicy: 'cache-and-network',
		variables: {
			accountAddress,
			...variables,
		},
	})

	const dataRef = ref(data.value?.accountByAddress?.rewards?.nodes?.[0])

	const componentState = useComponentState<AccountReward | undefined>({
		fetching,
		error,
		data: dataRef,
	})

	watch(
		() => data.value,
		value => (dataRef.value = value?.accountByAddress?.rewards?.nodes?.[0])
	)

	return { data, error, componentState }
}
