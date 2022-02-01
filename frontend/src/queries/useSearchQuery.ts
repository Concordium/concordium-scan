import { useQuery, gql } from '@urql/vue'
import { Ref } from 'vue'
import type { Block } from '~/types/blocks'
import type { Transaction } from '~/types/transactions'

type SearchResponse = {
	search: {
		blocks: Block[]
		transactions: Transaction[]
	}
}

const SearchQuery = gql<SearchResponse>`
	query Search($query: String!) {
		search(query: $query) {
			blocks {
				id
				blockHash
			}
			transactions {
				id
				transactionHash
			}
		}
	}
`

export const useSearchQuery = (query: Ref<string>) => {
	const { data } = useQuery({
		query: SearchQuery,
		requestPolicy: 'cache-first',
		variables: {
			query,
		},
	})
	return { data }
}
