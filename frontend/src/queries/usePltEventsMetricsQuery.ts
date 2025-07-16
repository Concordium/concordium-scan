import { useQuery, gql } from '@urql/vue'
import type { Ref } from 'vue'
import type { MetricsPeriod, PltEventMetrics } from '~/types/generated'
export type PltEventMetricsQueryResponse = {
	pltEventMetrics: PltEventMetrics
}

const PltEventMetricsQuery = gql<PltEventMetricsQueryResponse>`
	query PltEventMetrics($period: MetricsPeriod!) {
		pltEventMetrics(period: $period) {
			buckets {
				bucketWidth
				x_Time
				y_EventCount
				y_LastCumulativeEventCount
				y_TotalSupply
				y_TotalUniqueHolders
			}
			eventCount
			lastCumulativeEventCount
			lastCumulativeTotalSupply
			lastCumulativeUniqueHolders
			totalSupply
			totalUniqueHolders
		}
	}
`

export const usePltEventsMetricsQuery = (period: Ref<MetricsPeriod>) => {
	const { data, executeQuery, fetching } = useQuery({
		query: PltEventMetricsQuery,
		requestPolicy: 'cache-and-network',
		variables: { period },
	})

	return { data, executeQuery, loading: fetching }
}
