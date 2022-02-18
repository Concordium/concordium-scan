import { useQuery, gql } from '@urql/vue'
import { MetricsPeriod, TransactionMetrics } from '~/types/generated'

const TransactionMetricsQuery = gql<TransactionMetrics>`
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

export const useTransactionMetricsQuery = (period: MetricsPeriod) => {
	const { data, executeQuery } = useQuery({
		query: TransactionMetricsQuery,
		requestPolicy: 'cache-and-network',
		variables: { period },
	})

	return { data, executeQuery }
}
