import { useQuery, gql } from '@urql/vue'
import type { LatestTransactionResponse } from '~/types/generated'

export type stableCoinLatestTransactionsResponse = {
	latestTransactions: LatestTransactionResponse[]
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
		query: STABLECOIN_LATEST_TRANSACTIONS,
		requestPolicy: 'cache-and-network',
		variables: { limit },
	})

	return { data, executeQuery, fetching }
}
