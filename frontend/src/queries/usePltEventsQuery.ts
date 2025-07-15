import { useQuery, gql } from '@urql/vue'
import type { PageInfo, Pltevent } from '~/types/generated'

import type { QueryVariables } from '~/types/queryVariables'

export type PLTEventsQueryResponse = {
	pltEvents: {
		nodes: Pltevent[]
		pageInfo: PageInfo
	}
}

const PLT_TOKEN_QUERY = gql<PLTEventsQueryResponse>`
	query ($after: String, $before: String, $first: Int, $last: Int) {
		pltEvents(first: $first, last: $last, after: $after, before: $before) {
			nodes {
				id
				transactionIndex
				eventType
				tokenModuleType
				tokenId
				tokenName
				tokenEvent {
					... on BurnEvent {
						target {
							address {
								asString
							}
						}
						amount {
							value
							decimals
						}
					}
					... on MintEvent {
						target {
							address {
								asString
							}
						}
						amount {
							value
							decimals
						}
					}
					... on TokenTransferEvent {
						from {
							address {
								asString
							}
						}
						to {
							address {
								asString
							}
						}
						amount {
							value
							decimals
						}
						memo {
							bytes
						}
					}
					... on TokenModuleEvent {
						eventType
						details
					}
				}
				transactionHash
				block {
					blockSlotTime
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

export const usePltEventsQuery = (
	eventsVariables?: Partial<QueryVariables>
) => {
	const { data, fetching, error } = useQuery({
		query: PLT_TOKEN_QUERY,
		requestPolicy: 'cache-first',
		variables: eventsVariables,
	})

	return {
		data,
		error,
		loading: fetching,
	}
}
