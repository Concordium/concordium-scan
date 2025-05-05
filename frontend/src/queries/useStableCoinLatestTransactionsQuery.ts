import { useQuery, gql } from '@urql/vue'

export type Transaction = {
	assetName?: string
	transactionHash?: string
	from?: string
	to?: string
	amount?: number
	value?: number
	assetMetadata: {
		iconUrl?: string
	}
}

export type stableCoinLatestTransactionsResponse = {
	latestTransactions: Transaction[]
}

const STABLECOIN_LATEST_TRANSACTIONS = gql<stableCoinLatestTransactionsResponse>`
	query ($limit: Int!) {
		latestTransactions(limit: $limit) {
			from
			to
			assetName
			transactionHash
			amount
			value
			assetMetadata {
				iconUrl
			}
		}
	}
`
export const useStableCoinLatestTransactionsQuery = (limit: number) => {
	const { data, executeQuery, fetching } = useQuery({
		context: { url: useRuntimeConfig().public.apiUrlRust },
		query: STABLECOIN_LATEST_TRANSACTIONS,
		requestPolicy: 'cache-and-network',
		variables: { limit },
	})

	return { data, executeQuery, fetching }
}
