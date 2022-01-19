import { useQuery, gql } from '@urql/vue'
import type { Block } from '~/types/blocks'

type BlockResponse = {
	block: Block
}

const BlockQuery = gql<BlockResponse>`
	query ($id: ID!) {
		block(id: $id) {
			id
			blockHash
			bakerId
			blockSlotTime
			finalized
			transactionCount
			transactions {
				nodes {
					transactionHash
					senderAccountAddress
					ccdCost
					result {
						successful
					}
				}
			}
		}
	}
`

export const useBlockQuery = (id: string) => {
	const { data } = useQuery({
		query: BlockQuery,
		requestPolicy: 'cache-first',
		variables: {
			id,
		},
	})

	return { data }
}
