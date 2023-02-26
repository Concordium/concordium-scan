import { useQuery, gql } from '@urql/vue'
import { Ref } from 'vue'
import { BlockMetrics, MetricsPeriod } from '~/types/generated'

export type BlockMetricsQueryResponse = {
	blockMetrics: BlockMetrics
}

const BlockMetricsQuery = gql<BlockMetricsQueryResponse>`
	query ($period: MetricsPeriod!) {
		blockMetrics(period: $period) {
			lastBlockHeight
			blocksAdded
			avgBlockTime
			lastTotalMicroCcd
			lastTotalMicroCcdReleased
			lastTotalMicroCcdUnlocked
			avgFinalizationTime
			lastTotalMicroCcdStaked
			buckets {
				bucketWidth
				x_Time
				y_BlocksAdded
				y_BlockTimeMin
				y_BlockTimeAvg
				y_BlockTimeMax
				y_LastTotalMicroCcd
				y_FinalizationTimeAvg
				y_MinTotalMicroCcdStaked
				y_MaxTotalMicroCcdStaked
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
