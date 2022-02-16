import { useQuery, gql } from '@urql/vue'

type BlockMetricsResponse = {
	blockMetrics: {
		avgBlockTime: number
		blocksAdded: number
		buckets: {
			x_Time: Date[]
			y_BlocksAdded: number[]
			y_BlockTimeMin: number[]
			y_BlockTimeAvg: number[]
			y_BlockTimeMax: number[]
			y_LastTotalMicroCcd: number[]
			y_MinTotalEncryptedMicroCcd: number[]
			y_MaxTotalEncryptedMicroCcd: number[]
			y_LastTotalEncryptedMicroCcd: number[]
		}
		lastBlockHeight: number
		lastTotalEncryptedMicroCcd: number
		lastTotalMicroCcd: number
	}
}

const BlockMetricsQuery = gql<BlockMetricsResponse>`
	query {
		blockMetrics(period: LAST7_DAYS) {
			lastBlockHeight
			blocksAdded
			avgBlockTime
			lastTotalMicroCcd
			lastTotalEncryptedMicroCcd
			buckets {
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

export const useBlockMetricsQuery = () => {
	// variables: QueryVariables
	const { data } = useQuery({
		query: BlockMetricsQuery,
		requestPolicy: 'cache-and-network',
	})

	return { data }
}
