import { useQuery, gql } from '@urql/vue'
import { Ref } from 'vue'
import type { Block } from '~/types/blocks'
import type { Transaction } from '~/types/transactions'
import type { Account, PageInfo } from '~/types/generated'
type SearchResponse = {
	search: {
		blocks: { nodes: Block[]; pageInfo: PageInfo }
		transactions: { nodes: Transaction[]; pageInfo: PageInfo }
		accounts: { nodes: Account[]; pageInfo: PageInfo }
	}
}

const SearchQuery = gql<SearchResponse>`
	query Search($query: String!) {
		search(query: $query) {
			blocks(first: 3) {
				nodes {
					id
					blockHash
					blockHeight
					blockSlotTime
					transactionCount
				}
				pageInfo {
					hasNextPage
				}
			}
			transactions(first: 3) {
				nodes {
					id
					transactionHash
					block {
						blockHash
						blockHeight
						blockSlotTime
					}
				}
				pageInfo {
					hasNextPage
				}
			}
			accounts(first: 3) {
				nodes {
					id
					address
					createdAt
				}
				pageInfo {
					hasNextPage
				}
			}
		}
	}
`

export const useSearchQuery = (query: Ref<string>, paused = true) => {
	const { data, executeQuery } = useQuery({
		query: SearchQuery,
		requestPolicy: 'network-only',
		variables: {
			query,
		},
		pause: paused,
	})
	return { data, executeQuery }
}
