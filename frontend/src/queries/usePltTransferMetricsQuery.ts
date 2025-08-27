import { useQuery, gql } from '@urql/vue'
import type { Ref } from 'vue'
import type {
	MetricsPeriod,
	PltTransferMetricsByTokenId,
	GlobalPltMetrics,
} from '~/types/generated'

export type PltMetricsQueryResponse = {
	globalPltMetrics: GlobalPltMetrics
}

const PltMetricsQuery = gql<PltMetricsQueryResponse>`
	query GlobalPltMetrics($period: MetricsPeriod!) {
		globalPltMetrics(period: $period) {
			eventCount
			transferAmount
		}
	}
`

export const usePltMetricsQuery = (period: Ref<MetricsPeriod>) => {
	const { data, executeQuery, fetching } = useQuery({
		query: PltMetricsQuery,
		requestPolicy: 'cache-and-network',
		variables: { period },
	})

	return { data, executeQuery, loading: fetching }
}

export type PltTransferMetricsQueryResponse = {
	pltTransferMetricsByTokenId: PltTransferMetricsByTokenId
}

const PltTransferMetricsQueryByTokenId = gql<PltTransferMetricsQueryResponse>`
	query PltTransferMetricsByTokenId(
		$period: MetricsPeriod!
		$tokenId: String!
	) {
		pltTransferMetricsByTokenId(period: $period, tokenId: $tokenId) {
			transferCount
			transferAmount
			decimal
			buckets {
				bucketWidth
				x_Time
				y_TransferCount
				y_TransferAmount
			}
		}
	}
`

export const usePltTransferMetricsQueryByTokenId = (
	period: Ref<MetricsPeriod>,
	tokenId: Ref<string> | string
) => {
	const { data, executeQuery, fetching } = useQuery({
		query: PltTransferMetricsQueryByTokenId,
		requestPolicy: 'cache-and-network',
		variables: { period, tokenId },
	})

	return { data, executeQuery, loading: fetching }
}
