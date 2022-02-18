import { useQuery, gql } from '@urql/vue'
import { BlockMetrics, MetricsPeriod } from '~/types/generated'

const BlockMetricsQuery = gql<BlockMetrics>`
	query ($period: MetricsPeriod!) {
		blockMetrics(period: $period) {
			lastBlockHeight
			blocksAdded
			avgBlockTime
			lastTotalMicroCcd
			lastTotalEncryptedMicroCcd
			buckets {
				bucketWidth
				x_Time
				y_BlocksAdded
				y_BlockTimeMin
				y_BlockTimeAvg
				y_BlockTimeMax
				y_LastTotalMicroCcd
				y_MinTotalEncryptedMicroCcd
				y_MaxTotalEncryptedMicroCcd
				y_LastTotalEncryptedMicroCcd
			}
		}
	}
`

export const useBlockMetricsQuery = (period: MetricsPeriod) => {
	const { data, executeQuery } = useQuery({
		query: BlockMetricsQuery,
		requestPolicy: 'cache-and-network',
		variables: { period },
	})

	return { data, executeQuery }
}
