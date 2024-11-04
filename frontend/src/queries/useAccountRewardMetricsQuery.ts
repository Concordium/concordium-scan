import { useQuery, gql } from '@urql/vue'
import type { Ref } from 'vue'
import type { Account, MetricsPeriod, RewardMetrics } from '~/types/generated'

export type AccountRewardMetricsQueryResponse = {
	rewardMetricsForAccount: RewardMetrics
}

const AccountRewardMetricsQuery = gql<AccountRewardMetricsQueryResponse>`
	query ($accountId: ID!, $period: MetricsPeriod!) {
		rewardMetricsForAccount(accountId: $accountId, period: $period) {
			sumRewardAmount
			buckets {
				bucketWidth
				x_Time
				y_SumRewards
			}
		}
	}
`

export const useAccountRewardMetricsQuery = (
	accountId: Account['id'],
	period: Ref<MetricsPeriod>
) => {
	const { data, fetching } = useQuery({
		query: AccountRewardMetricsQuery,
		requestPolicy: 'cache-and-network',
		variables: { accountId, period },
	})

	return { data, fetching }
}
