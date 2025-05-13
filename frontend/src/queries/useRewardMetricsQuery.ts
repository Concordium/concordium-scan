import { useQuery, gql } from '@urql/vue'
import type { Ref } from 'vue'
import type { RewardMetrics, MetricsPeriod } from '~/types/generated'

export type RewardMetricsQueryResponse = {
	rewardMetrics: RewardMetrics
}

const RewardMetricsQuery = gql<RewardMetricsQueryResponse>`
	query ($period: MetricsPeriod!) {
		rewardMetrics(period: $period) {
			sumRewardAmount
			buckets {
				bucketWidth
				x_Time
				y_SumRewards
			}
		}
	}
`

export const useRewardMetricsQuery = (period: Ref<MetricsPeriod>) => {
	const { data, fetching } = useQuery({
		query: RewardMetricsQuery,
		requestPolicy: 'cache-and-network',
		variables: { period },
	})

	return { data, fetching }
}
