import { useQuery, gql } from '@urql/vue'
import { Ref } from 'vue'
import type { BakerMetrics, MetricsPeriod } from '~/types/generated'

export type BakerMetricsQueryResponse = {
	bakerMetrics: BakerMetrics
}

const BakerMetricsQuery = gql<BakerMetricsQueryResponse>`
	query ($period: MetricsPeriod!) {
		bakerMetrics(period: $period) {
			lastBakerCount
			bakersAdded
			bakersRemoved
			buckets {
				bucketWidth
				x_Time
				y_LastBakerCount
				y_BakersAdded
				y_BakersRemoved
			}
		}
	}
`

export const useBakerMetricsQuery = (period: Ref<MetricsPeriod>) => {
	const { data, fetching } = useQuery({
		query: BakerMetricsQuery,
		requestPolicy: 'cache-and-network',
		variables: { period },
	})

	return { data, fetching }
}
