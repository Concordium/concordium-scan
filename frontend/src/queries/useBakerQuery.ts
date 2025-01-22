import { useQuery, gql } from '@urql/vue'
import { useComponentState } from '~/composables/useComponentState'
import type {
	Baker,
	BakerState,
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
	paydayStatus: {
		nextPaydayTime: string
	}
	latestChainParameters: {
		rewardPeriodLength: number
	}
	importState: {
		epochDuration: number
	}
}

const BakerQuery = gql<BakerResponse>`
	query ($bakerId: Long!) {
		paydayStatus {
			nextPaydayTime
		}

		latestChainParameters {
			__typename
			rewardPeriodLength
		}

		importState {
			epochDuration
		}

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
					nodeStatus {
						id
						nodeId
						nodeName
						averagePing
						uptime
						clientVersion
					}
					restakeEarnings
					pool {
						openStatus
						totalStakePercentage
						delegatorCount
						totalStake
						delegatedStake
						lotteryPower
						metadataUrl
						rankingByTotalStake {
							rank
							total
						}
						paydayCommissionRates {
							transactionCommission
							bakingCommission
						}
						commissionRates {
							transactionCommission
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

	const dataRef = ref(data.value?.bakerByBakerId)

	const componentState = useComponentState<BakerWithAPYFilter | undefined>({
		fetching,
		error,
		data: dataRef,
	})

	watch(
		() => data.value,
		value => (dataRef.value = value?.bakerByBakerId)
	)

	return { data, error, componentState }
}
