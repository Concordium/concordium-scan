import { useQuery, gql } from '@urql/vue'
import type { Ref } from 'vue'
import type { MetricsPeriod, PltEventMetrics } from '~/types/generated'
export type PltEventMetricsQueryResponse = {
	pltEventMetrics: PltEventMetrics
}

const PltEventMetricsQuery = gql<PltEventMetricsQueryResponse>`
	query PltEventMetrics($period: MetricsPeriod!) {
		pltEventMetrics(period: $period) {
			transferCount
			transferVolume
			mintCount
			mintVolume
			burnCount
			burnVolume
			tokenModuleCount
			totalEventCount
			buckets {
				bucketWidth
				x_Time
				y_TransferCount
				y_TransferVolume
				y_MintCount
				y_MintVolume
				y_BurnCount
				y_BurnVolume
				y_TokenModuleCount
				y_TotalEventCount
			}
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
