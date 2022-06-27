import { useQuery, gql } from '@urql/vue'
import { useComponentState } from '~/composables/useComponentState'
import type { QueryVariables } from '~/types/queryVariables'
import type { NodeStatus, PageInfo } from '~/types/generated'

type NodeResponse = {
	nodeStatuses: {
		nodes: NodeStatus[]
		pageInfo: PageInfo
	}
}

const BakerQuery = gql<NodeResponse>`
	query ($after: String, $before: String, $first: Int, $last: Int) {
		nodeStatuses(after: $after, before: $before, first: $first, last: $last) {
			nodes {
				id
				nodeId
				nodeName
				uptime
				peersCount
				averagePing
				clientVersion
				consensusBakerId
				finalizedBlockHeight
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

export const useNodeQuery = (variables: Partial<QueryVariables>) => {
	const { data, fetching, error } = useQuery({
		query: BakerQuery,
		requestPolicy: 'cache-and-network',
		variables,
	})

	const componentState = useComponentState<NodeResponse | undefined>({
		fetching,
		error,
		data,
	})

	return { data, error, componentState }
}
