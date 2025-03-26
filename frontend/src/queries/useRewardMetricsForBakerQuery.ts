import { useQuery, gql } from '@urql/vue'
import type { Ref } from 'vue'
import type { RewardMetrics, MetricsPeriod } from '~/types/generated'

export type RewardMetricsForBakerQueryResponse = {
	rewardMetricsForBaker: RewardMetrics
}

const RewardMetricsForBakerQuery = gql<RewardMetricsForBakerQueryResponse>`
	query ($bakerId: Long!, $period: MetricsPeriod!) {
		rewardMetricsForBaker(bakerId: $bakerId, period: $period) {
			sumRewardAmount
			buckets {
				bucketWidth
				x_Time
				y_SumRewards
			}
		}
	}
`

export const useRewardMetricsForBakerQueryQuery = (
	bakerId: Ref<number>,
	period: Ref<MetricsPeriod>
) => {
	const { data, executeQuery, fetching } = useQuery({
		context: { url: useRuntimeConfig().public.apiUrlRust },
		query: RewardMetricsForBakerQuery,
		requestPolicy: 'cache-and-network',
		variables: { bakerId, period },
	})

	return { data, executeQuery, fetching }
}
