import { useQuery, gql } from '@urql/vue'
import type { Ref } from 'vue'
import type {
	MetricsPeriod,
	PltTransferMetrics,
	PltMetrics,
} from '~/types/generated'

export type PltMetricsQueryResponse = {
	pltMetrics: PltMetrics
}

const PltMetricsQuery = gql<PltMetricsQueryResponse>`
	query PltMetrics($period: MetricsPeriod!) {
		pltMetrics(period: $period) {
			transactionCount
			transferVolume
			uniqueAccounts
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
	pltTransferMetrics: PltTransferMetrics
}

const PltTransferMetricsQueryByTokenId = gql<PltTransferMetricsQueryResponse>`
	query PltTransferMetrics($period: MetricsPeriod!, $tokenId: String!) {
		pltTransferMetrics(period: $period, tokenId: $tokenId) {
			transferCount
			transferVolume
			decimal
			buckets {
				bucketWidth
				x_Time
				y_TransferCount
				y_TransferVolume
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
