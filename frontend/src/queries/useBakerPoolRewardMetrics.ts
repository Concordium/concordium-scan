import { useQuery, gql } from '@urql/vue'
import type { Ref } from 'vue'
import type { Baker, MetricsPeriod, PoolRewardMetrics } from '~/types/generated'

export type PoolRewardMetricsForBakerPoolResponse = {
	poolRewardMetricsForBakerPool: PoolRewardMetrics
}

const PoolRewardMetricsForBakerPoolQuery = gql<PoolRewardMetricsForBakerPoolResponse>`
	query ($bakerId: ID!, $period: MetricsPeriod!) {
		poolRewardMetricsForBakerPool(bakerId: $bakerId, period: $period) {
			sumTotalRewardAmount
			sumBakerRewardAmount
			sumDelegatorsRewardAmount
			buckets {
				bucketWidth
				x_Time
				y_SumTotalRewards
				y_SumBakerRewards
				y_SumDelegatorsRewards
			}
		}
	}
`

export const useBakerPoolRewardMetrics = (
	bakerId: Baker['id'],
	period: Ref<MetricsPeriod>
) => {
	const { data, executeQuery, fetching } = useQuery({
		query: PoolRewardMetricsForBakerPoolQuery,
		requestPolicy: 'cache-and-network',
		variables: { bakerId, period },
	})

	return { data, executeQuery, fetching }
}
