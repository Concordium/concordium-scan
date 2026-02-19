import { useQuery, gql } from '@urql/vue'
import type { PageInfo, PltAccountAmount, Scalars } from '~/types/generated'

import type { QueryVariables } from '~/types/queryVariables'

export type PltTokenHolderQueryResponse = {
	pltAccountsByTokenId: {
		nodes: PltAccountAmount[]
		pageInfo: PageInfo
	}
}

const PLT_HOLDER_BY_TOKEN_ID = gql<PltTokenHolderQueryResponse>`
	query PltAccountsByTokenId(
		$after: String
		$before: String
		$first: Int
		$last: Int
		$tokenId: String
	) {
		pltAccountsByTokenId(
			first: $first
			last: $last
			after: $after
			before: $before
			tokenId: $tokenId
		) {
			nodes {
				accountAddress {
					asString
				}
				tokenId
				amount {
					value
					decimals
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

export const usePltTokenHolderQuery = (
	tokenId: Scalars['ID'],
	eventsVariables?: Partial<QueryVariables>
) => {
	const { data, fetching, error } = useQuery({
		query: PLT_HOLDER_BY_TOKEN_ID,
		requestPolicy: 'network-only',
		variables: {
			tokenId: tokenId,
			...eventsVariables,
		},
	})

	return {
		data,
		fetching,
		error,
	}
}
