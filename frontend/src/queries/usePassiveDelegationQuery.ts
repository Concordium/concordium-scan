import { useQuery, gql } from '@urql/vue'
import { useComponentState } from '~/composables/useComponentState'
import type { PassiveDelegation } from '~/types/generated'
import type { QueryVariables } from '~/types/queryVariables'

export type PassiveDelegationWithAPYFilter = PassiveDelegation & {
	apy7days: PassiveDelegation['apy']
	apy30days: PassiveDelegation['apy']
}

type PassiveDelegationResponse = {
	passiveDelegation: PassiveDelegationWithAPYFilter
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
	query PassiveDelegation(
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
			poolRewards(
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
			}
			commissionRates {
				transactionCommission
				finalizationCommission
				bakingCommission
			}
			apy7days: apy(period: LAST7_DAYS)
			apy30days: apy(period: LAST30_DAYS)
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
	const componentState = useComponentState<
		PassiveDelegationWithAPYFilter | undefined
	>({
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
