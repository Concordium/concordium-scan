import { useQuery, gql } from '@urql/vue'
import { Ref } from 'vue'
import type { Block } from '~/types/blocks'
import type { Transaction } from '~/types/transactions'
import type { Account } from '~/types/generated'
type SearchResponse = {
	search: {
		blocks: Block[]
		transactions: Transaction[]
		accounts: Account[]
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
			accounts {
				id
				address
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
