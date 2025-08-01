import { useQuery, gql } from '@urql/vue'
import type { PageInfo, PltaccountAmount, Scalars } from '~/types/generated'

import type { QueryVariables } from '~/types/queryVariables'

export type PLTTokenHolderQueryResponse = {
	pltAccountsByTokenId: {
		nodes: PltaccountAmount[]
		pageInfo: PageInfo
	}
}

const PLT_HOLDER_BY_TOKEN_ID = gql<PLTTokenHolderQueryResponse>`
	query (
		$after: String
		$before: String
		$first: Int
		$last: Int
		$id: String
	) {
		pltAccountsByTokenId(
			first: $first
			last: $last
			after: $after
			before: $before
			id: $id
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
	id: Scalars['ID'],
	eventsVariables?: Partial<QueryVariables>
) => {
	const { data, fetching, error } = useQuery({
		query: PLT_HOLDER_BY_TOKEN_ID,
		requestPolicy: 'network-only',
		variables: {
			id,
			...eventsVariables,
		},
	})

	return {
		data,
		fetching,
		error,
	}
}
