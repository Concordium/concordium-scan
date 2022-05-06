import { useQuery, gql } from '@urql/vue'
import type { PageInfo, Block } from '~/types/generated'
import type { QueryVariables } from '~/types/queryVariables'

type BlockListResponse = {
	blocks: {
		nodes: Block[]
		pageInfo: PageInfo
	}
}

const BlocksQuery = gql<BlockListResponse>`
	query ($after: String, $before: String, $first: Int, $last: Int) {
		blocks(after: $after, before: $before, first: $first, last: $last) {
			nodes {
				id
				bakerId
				blockHash
				blockHeight
				blockSlotTime
				finalized
				transactionCount
				specialEventsOld {
					blockRewards {
						bakerReward
					}
				}
			}
			pageInfo {
				startCursor
				endCursor
				hasPreviousPage
				hasNextPage
			}
		}
	}
`

export const useBlockListQuery = (variables: Partial<QueryVariables>) => {
	const { data, executeQuery } = useQuery({
		query: BlocksQuery,
		requestPolicy: 'cache-and-network',
		variables,
	})

	return { data, executeQuery }
}
