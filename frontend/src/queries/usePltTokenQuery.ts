import { useQuery, gql } from '@urql/vue'
import type { Plttoken } from '~/types/generated'

import type { QueryVariables } from '~/types/queryVariables'

export type PLTTokenQueryResponse = {
	pltTokens: {
		nodes: Plttoken[]
	}
}

const PLT_TOKEN_QUERY = gql<PLTTokenQueryResponse>`
	query {
		pltTokens {
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
			}
		}
	}
`

function getData(value: PLTTokenQueryResponse | undefined | null): Plttoken[] {
	return value?.pltTokens?.nodes ?? []
}

export const usePltTokenQuery = (eventsVariables?: QueryVariables) => {
	const { data, fetching, error } = useQuery({
		query: PLT_TOKEN_QUERY,
		requestPolicy: 'cache-and-network',
		variables: eventsVariables,
	})

	const dataRef = ref<Plttoken[]>(getData(data.value))
	const componentState = useComponentState<Plttoken[]>({
		fetching,
		error,
		data: dataRef,
	})

	watch(
		() => data.value,
		value => (dataRef.value = getData(value))
	)

	return {
		data: dataRef,
		error,
		componentState,
		loading: fetching,
	}
}
