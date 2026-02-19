import { useQuery, gql } from '@urql/vue'
import type { Ref } from 'vue'
import type { AccountsMetrics, MetricsPeriod } from '~/types/generated'

export type AccountsMetricsQueryResponse = {
	accountsMetrics: AccountsMetrics
}

const AccountsMetricsQuery = gql<AccountsMetricsQueryResponse>`
	query AccountsMetrics($period: MetricsPeriod!) {
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
	const { data, fetching, executeQuery } = useQuery({
		query: AccountsMetricsQuery,
		requestPolicy: 'cache-and-network',
		variables: { period },
	})

	return { data, fetching, executeQuery }
}
