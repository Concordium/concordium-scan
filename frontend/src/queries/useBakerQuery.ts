import { useQuery, gql } from '@urql/vue'
import { useComponentState } from '~/composables/useComponentState'
import type {
	Baker,
	BakerState,
	BakerPool,
	ActiveBakerState,
	PoolApy,
} from '~/types/generated'

export type BakerWithAPYFilter = Baker & {
	state: BakerState & {
		pool?: ActiveBakerState['pool'] & {
			apy7days: PoolApy
			apy30days: PoolApy
		}
	}
}

type BakerResponse = {
	bakerByBakerId: BakerWithAPYFilter
}

const BakerQuery = gql<BakerResponse>`
	query ($bakerId: Long!) {
		bakerByBakerId(bakerId: $bakerId) {
			id
			bakerId
			account {
				id
				address {
					asString
				}
			}
			state {
				__typename
				... on ActiveBakerState {
					stakedAmount
					restakeEarnings
					pool {
						openStatus
						totalStakePercentage
						delegatorCount
						totalStake
						delegatedStake
						metadataUrl
						rankingByTotalStake {
							rank
							total
						}
						commissionRates {
							transactionCommission
							finalizationCommission
							bakingCommission
						}
						apy7days: apy(period: LAST7_DAYS) {
							bakerApy
							delegatorsApy
							totalApy
						}
						apy30days: apy(period: LAST30_DAYS) {
							bakerApy
							delegatorsApy
							totalApy
						}
					}
					pendingChange {
						... on PendingBakerReduceStake {
							__typename
							effectiveTime
							newStakedAmount
						}
						... on PendingBakerRemoval {
							__typename
							effectiveTime
						}
					}
				}
				... on RemovedBakerState {
					removedAt
				}
			}
		}
	}
`

export const useBakerQuery = (bakerId: number) => {
	const { data, fetching, error } = useQuery({
		query: BakerQuery,
		requestPolicy: 'cache-first',
		variables: {
			bakerId,
		},
	})

	const componentState = useComponentState<BakerResponse | undefined>({
		fetching,
		error,
		data,
	})

	return { data, error, componentState }
}
