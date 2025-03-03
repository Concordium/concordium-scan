import { useQuery, gql } from '@urql/vue'
import type { Ref } from 'vue'
import { useComponentState } from '~/composables/useComponentState'
import type { QueryVariables } from '~/types/queryVariables'
import type {
	NodeSortDirection,
	NodeSortField,
	NodeStatus,
	PageInfo,
} from '~/types/generated'

type NodeResponse = {
	nodeStatuses: {
		nodes: NodeStatus[]
		pageInfo: PageInfo
	}
}

const BakerQuery = gql<NodeResponse>`
	query (
		$sortField: NodeSortField!
		$sortDirection: NodeSortDirection!
		$after: String
		$before: String
		$first: Int
		$last: Int
	) {
		nodeStatuses(
			sortField: $sortField
			sortDirection: $sortDirection
			after: $after
			before: $before
			first: $first
			last: $last
		) {
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
				blocksReceivedCount
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

export const useNodeQuery = (
	sortField: Ref<NodeSortField>,
	sortDirection: Ref<NodeSortDirection>,
	variables: Partial<QueryVariables>
) => {
	const { data, fetching, error } = useQuery({
		context: { url: useRuntimeConfig().public.apiUrlRust },
		query: BakerQuery,
		requestPolicy: 'cache-and-network',
		variables: { sortField, sortDirection, ...variables },
	})

	const componentState = useComponentState<NodeResponse | undefined>({
		fetching,
		error,
		data,
	})

	return { data, error, componentState }
}
