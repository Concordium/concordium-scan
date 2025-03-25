import { useQuery, gql } from '@urql/vue'
import type { Ref } from 'vue'
import type { MetricsPeriod, PoolRewardMetrics } from '~/types/generated'

export type PoolRewardMetricsForPassiveDelegationResponse = {
	poolRewardMetricsForPassiveDelegation: PoolRewardMetrics
}

const PoolRewardMetricsForPassiveDelegationQuery = gql<PoolRewardMetricsForPassiveDelegationResponse>`
	query ($period: MetricsPeriod!) {
		poolRewardMetricsForPassiveDelegation(period: $period) {
			sumTotalRewardAmount
			sumBakerRewardAmount
			sumDelegatorsRewardAmount
			buckets {
				bucketWidth
				x_Time
				y_SumTotalRewards
			}
		}
	}
`

export const usePassiveDelegationPoolRewardMetrics = (
	period: Ref<MetricsPeriod>
) => {
	const { data, executeQuery, fetching } = useQuery({
		context: { url: useRuntimeConfig().public.apiUrlRust },
		query: PoolRewardMetricsForPassiveDelegationQuery,
		requestPolicy: 'cache-and-network',
		variables: { period },
	})

	return { data, executeQuery, fetching }
}
