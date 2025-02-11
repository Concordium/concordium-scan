import { useQuery, gql } from '@urql/vue'
import type { Ref } from 'vue'
import type { MetricsPeriod, TransactionMetrics } from '~/types/generated'
export type TransactionMetricsQueryResponse = {
	transactionMetrics: TransactionMetrics
}

const TransactionMetricsQuery = gql<TransactionMetricsQueryResponse>`
	query ($period: MetricsPeriod!) {
		transactionMetrics(period: $period) {
			lastCumulativeTransactionCount
			transactionCount
			buckets {
				bucketWidth
				x_Time
				y_LastCumulativeTransactionCount
				y_TransactionCount
			}
		}
	}
`

export const useTransactionMetricsQuery = (period: Ref<MetricsPeriod>) => {
	const { data, executeQuery, fetching } = useQuery({
		context: { url: useRuntimeConfig().public.apiUrlRust },
		query: TransactionMetricsQuery,
		requestPolicy: 'cache-and-network',
		variables: { period },
	})

	return { data, executeQuery, fetching }
}
