import { useQuery, gql } from '@urql/vue'
import type { PltToken, Scalars } from '~/types/generated'

import type { QueryVariables } from '~/types/queryVariables'

export type PltTokenQueryResponse = {
	pltTokens: {
		nodes: PltToken[]
	}
}

const PLT_TOKEN_QUERY = gql<PltTokenQueryResponse>`
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

function getData(value: PltTokenQueryResponse | undefined | null): PltToken[] {
	return value?.pltTokens?.nodes ?? []
}

export const usePltTokenQuery = (eventsVariables?: QueryVariables) => {
	const { data, fetching, error } = useQuery({
		query: PLT_TOKEN_QUERY,
		requestPolicy: 'cache-and-network',
		variables: eventsVariables,
	})

	const dataRef = ref<PltToken[]>(getData(data.value))
	const componentState = useComponentState<PltToken[]>({
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

export type PltTokenQueryByTokenIdResponse = {
	pltToken: PltToken
}

const PLT_TOKEN_QUERY_BY_ID = gql<PltTokenQueryByTokenIdResponse>`
	query ($id: String!) {
		pltToken(id: $id) {
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
			totalUniqueHolders
			issuer {
				asString
			}
			tokenCreationDetails {
				createPlt {
					initializationParameters {
						allowList
						denyList
						mintable
						burnable
					}
				}
			}
			tokenModulePaused
		}
	}
`

export const usePltTokenQueryById = (tokenId: string) => {
	const { data, fetching, error } = useQuery({
		query: PLT_TOKEN_QUERY_BY_ID,
		requestPolicy: 'cache-and-network',
		variables: { id: tokenId },
	})

	const dataRef = ref<PltToken | null>(data.value?.pltToken ?? null)
	const componentState = useComponentState<PltToken | null>({
		fetching,
		error,
		data: dataRef,
	})

	watch(
		() => data.value,
		value => (dataRef.value = value?.pltToken ?? null)
	)

	return {
		data: dataRef,
		error,
		componentState,
		loading: fetching,
	}
}

export type UniqueHolders = {
	pltUniqueAccounts: Scalars['Int']
}

const PLT_ACCOUNT_AMOUNT_QUERY = gql<UniqueHolders>`
	query PltUniqueAccounts {
		pltUniqueAccounts
	}
`

export const usePltUniqueAccountsQuery = () => {
	const { data, fetching, error } = useQuery({
		query: PLT_ACCOUNT_AMOUNT_QUERY,
		requestPolicy: 'cache-and-network',
	})

	const dataRef = ref<UniqueHolders | null>(data.value ?? null)
	const componentState = useComponentState<UniqueHolders | null>({
		fetching,
		error,
		data: dataRef,
	})

	watch(
		() => data.value,
		value => (dataRef.value = value ?? null)
	)

	return {
		data: dataRef,
		error,
		componentState,
		loading: fetching,
	}
}
