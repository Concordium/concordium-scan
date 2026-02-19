import { useQuery, gql } from '@urql/vue'
import type { Ref } from 'vue'
import type { BlockMetrics, MetricsPeriod } from '~/types/generated'

export type BlockMetricsQueryResponse = {
	blockMetrics: BlockMetrics
}

const BlockMetricsQuery = gql<BlockMetricsQueryResponse>`
	query BlockMetrics($period: MetricsPeriod!) {
		blockMetrics(period: $period) {
			lastBlockHeight
			blocksAdded
			avgBlockTime
			lastTotalMicroCcd
			lastTotalMicroCcdReleased
			avgFinalizationTime
			lastTotalMicroCcdStaked
			buckets {
				bucketWidth
				x_Time
				y_BlocksAdded
				y_BlockTimeAvg
				y_FinalizationTimeAvg
				y_LastTotalMicroCcdStaked
			}
		}
	}
`

export const useBlockMetricsQuery = (period: Ref<MetricsPeriod>) => {
	const { data, executeQuery, fetching } = useQuery({
		query: BlockMetricsQuery,
		requestPolicy: 'cache-and-network',
		variables: { period },
	})

	return { data, executeQuery, fetching }
}
