import { useQuery, gql } from '@urql/vue'
import { Ref } from 'vue'
import { MetricsPeriod, TransactionMetrics } from '~/types/generated'
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
	const { data, executeQuery } = useQuery({
		query: TransactionMetricsQuery,
		requestPolicy: 'cache-and-network',
		variables: { period },
	})

	return { data, executeQuery }
}
