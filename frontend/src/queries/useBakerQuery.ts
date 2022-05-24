import { useQuery, gql } from '@urql/vue'
import { useComponentState } from '~/composables/useComponentState'
import type { Baker } from '~/types/generated'

type BakerResponse = {
	bakerByBakerId: Baker
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
						totalStakePercentage
						delegatorCount
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
