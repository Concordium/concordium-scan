import { useQuery, gql } from '@urql/vue'
import type { PageInfo, Token } from '~/types/generated'
import type { QueryVariables } from '~/types/queryVariables'

type ListResponse = {
	tokens: {
		nodes: Token[]
		pageInfo: PageInfo
	}
}

type ListVariables = Partial<QueryVariables>

const TokensQuery = gql`
	query ($after: String, $before: String, $first: Int, $last: Int) {
		tokens(after: $after, before: $before, first: $first, last: $last) {
			nodes {
				contractIndex
				contractSubIndex
				tokenId
				metadataUrl
				totalSupply
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

export const useTokensListQuery = (variables: ListVariables) => {
	const { data } = useQuery<ListResponse, ListVariables>({
		query: TokensQuery,
		requestPolicy: 'cache-and-network',
		variables,
	})

	return { data }
}
