import { useQuery, gql } from '@urql/vue'
import { Ref } from 'vue'
import { MetricsPeriod, PoolRewardMetrics } from '~/types/generated'

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
		query: PoolRewardMetricsForPassiveDelegationQuery,
		requestPolicy: 'cache-and-network',
		variables: { period },
	})

	return { data, executeQuery, fetching }
}
