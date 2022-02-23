import { useQuery, gql } from '@urql/vue'
import { Ref } from 'vue'
import type { AccountsMetrics, MetricsPeriod } from '~/types/generated'

type AccountsMetricsQueryResponse = {
	accountsMetrics: AccountsMetrics
}

const AccountsMetricsQuery = gql<AccountsMetricsQueryResponse>`
	query ($period: MetricsPeriod!) {
		accountsMetrics(period: $period) {
			lastCumulativeAccountsCreated
			accountsCreated
			buckets {
				bucketWidth
				x_Time
				y_LastCumulativeAccountsCreated
				y_AccountsCreated
			}
		}
	}
`

export const useAccountsMetricsQuery = (period: Ref<MetricsPeriod>) => {
	const { data } = useQuery({
		query: AccountsMetricsQuery,
		requestPolicy: 'cache-and-network',
		variables: { period },
	})

	return { data }
}
