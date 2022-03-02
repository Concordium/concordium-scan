import { useQuery, gql } from '@urql/vue'
import { Ref } from 'vue'
import type { Block } from '~/types/blocks'
import type { Transaction } from '~/types/transactions'
import type { Account } from '~/types/generated'
type SearchResponse = {
	search: {
		blocks: { nodes: Block[] }
		transactions: { nodes: Transaction[] }
		accounts: { nodes: Account[] }
	}
}

const SearchQuery = gql<SearchResponse>`
	query Search($query: String!) {
		search(query: $query) {
			blocks {
				nodes {
					id
					blockHash
					transactions {
						nodes {
							transactionHash
							id
						}
					}
				}
			}
			transactions {
				nodes {
					id
					transactionHash
				}
			}
			accounts {
				nodes {
					id
					address
					transactions {
						nodes {
							transaction {
								transactionHash
								id
							}
						}
					}
				}
			}
		}
	}
`

export const useSearchQuery = (query: Ref<string>, paused = true) => {
	const { data, executeQuery } = useQuery({
		query: SearchQuery,
		requestPolicy: 'cache-first',
		variables: {
			query,
		},
		pause: paused,
	})
	return { data, executeQuery }
}
