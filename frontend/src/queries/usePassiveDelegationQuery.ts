import { useQuery, gql } from '@urql/vue'
import { useComponentState } from '~/composables/useComponentState'
import type { PassiveDelegation } from '~/types/generated'
import type { QueryVariables } from '~/types/queryVariables'

type PassiveDelegationResponse = {
	passiveDelegation: PassiveDelegation
}
type PassiveDelegationQueryVariables = {
	firstDelegators: QueryVariables['first']
	lastDelegators: QueryVariables['last']
	afterDelegators: QueryVariables['after']
	beforeDelegators: QueryVariables['before']
	firstRewards: QueryVariables['first']
	lastRewards: QueryVariables['last']
	afterRewards: QueryVariables['after']
	beforeRewards: QueryVariables['before']
}

const PassiveDelegationQuery = gql<PassiveDelegationResponse>`
	query (
		$afterDelegators: String
		$beforeDelegators: String
		$firstDelegators: Int
		$lastDelegators: Int
		$afterRewards: String
		$beforeRewards: String
		$firstRewards: Int
		$lastRewards: Int
	) {
		passiveDelegation {
			delegators(
				after: $afterDelegators
				before: $beforeDelegators
				first: $firstDelegators
				last: $lastDelegators
			) {
				nodes {
					accountAddress {
						asString
					}
					stakedAmount
					restakeEarnings
				}
				pageInfo {
					hasNextPage
					hasPreviousPage
					startCursor
					endCursor
				}
			}
			rewards(
				after: $afterRewards
				before: $beforeRewards
				first: $firstRewards
				last: $lastRewards
			) {
				pageInfo {
					hasNextPage
					hasPreviousPage
					startCursor
					endCursor
				}
				nodes {
					block {
						blockHash
					}
					id
					totalAmount
					delegatorsAmount
					bakerAmount
					rewardType
					timestamp
				}
			}
			commissionRates {
				transactionCommission
				finalizationCommission
				bakingCommission
			}

			delegatorCount
			delegatedStake
			delegatedStakePercentage
		}
	}
`

export const usePassiveDelegationQuery = (
	pagingVariables: PassiveDelegationQueryVariables
) => {
	const { data, fetching, error } = useQuery({
		query: PassiveDelegationQuery,
		requestPolicy: 'cache-first',
		variables: {
			...pagingVariables,
		},
	})

	const dataRef = ref(data.value?.passiveDelegation)
	const componentState = useComponentState<PassiveDelegation | undefined>({
		fetching,
		error,
		data: dataRef,
	})
	watch(
		() => data.value,
		value => (dataRef.value = value?.passiveDelegation)
	)
	return { data: dataRef, error, componentState }
}
