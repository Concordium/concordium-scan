import { useQuery, gql } from '@urql/vue'
import type {
	PltToken,
	PageInfo,
	PltTokenSort,
	PltTokenFilterInput,
} from '~/types/generated'
import type { Ref } from 'vue'

export type PltTokensQueryVariables = {
	after: Ref<string | null | undefined>
	before: Ref<string | null | undefined>
	first: Ref<number | undefined>
	last: Ref<number | undefined>
	sort?: Ref<PltTokenSort>
	filter?: Ref<PltTokenFilterInput | undefined>
}

export type PltTokensPagedQueryResponse = {
	pltTokens: {
		nodes: PltToken[]
		pageInfo: PageInfo
	}
}

const PLT_TOKENS_PAGED_QUERY = gql<PltTokensPagedQueryResponse>`
	query (
		$after: String
		$before: String
		$first: Int
		$last: Int
		$sort: PltTokenSort
		$filter: PltTokenFilterInput
	) {
		pltTokens(
			first: $first
			last: $last
			after: $after
			before: $before
			sort: $sort
			filter: $filter
		) {
			nodes {
				name
				tokenId
				transactionHash
				moduleReference
				metadata
				initialSupply
				totalSupply
				totalMinted
				totalBurned
				decimal
				index
				issuer {
					asString
				}
				totalUniqueHolders
				normalizedCurrentSupply
				block {
					blockSlotTime
				}
			}
			pageInfo {
				hasNextPage
				hasPreviousPage
				startCursor
				endCursor
			}
		}
	}
`

function getData(
	value: PltTokensPagedQueryResponse | undefined | null
): PltToken[] {
	return value?.pltTokens?.nodes ?? []
}

function getPageInfo(
	value: PltTokensPagedQueryResponse | undefined | null
): PageInfo | null {
	return value?.pltTokens?.pageInfo ?? null
}

export const usePltTokensPagedQuery = (
	eventsVariables?: PltTokensQueryVariables
) => {
	const { data, fetching, error } = useQuery({
		query: PLT_TOKENS_PAGED_QUERY,
		requestPolicy: 'cache-and-network',
		variables: eventsVariables,
	})

	const dataRef = ref<PltToken[]>(getData(data.value))
	const pageInfoRef = ref<PageInfo | null>(getPageInfo(data.value))

	const componentState = useComponentState<PltToken[]>({
		fetching,
		error,
		data: dataRef,
	})

	watch(
		() => data.value,
		value => {
			dataRef.value = getData(value)
			pageInfoRef.value = getPageInfo(value)
		}
	)

	return {
		data: dataRef,
		pageInfo: pageInfoRef,
		error,
		componentState,
		loading: fetching,
	}
}
