import { useQuery, gql } from '@urql/vue'

// Define DailySummaryItem
export type DailySummaryItem = {
	date: string
	totalAmount: string
	transactionCount: number
}

// Define main response type
export type StableCoinTokenTransferResponse = {
	stablecoin: {
		name?: string
		totalSupply?: string
		totalUniqueHolder?: number
		valueInDoller?: number
		decimal?: number
		symbol?: string
		metadata?: {
			iconUrl?: string
		}
	}
	transferSummary?: {
		dailySummary?: DailySummaryItem[]
	}
}

// GraphQL Query
const STABLECOIN_TOKEN_TRANSFER = gql<StableCoinTokenTransferResponse>`
	query ($symbol: String!, $days: Int!) {
		stablecoin(symbol: $symbol) {
			name
			totalSupply
			totalUniqueHolders
			valueInDollar
			decimal
			symbol
			issuer
			metadata {
				iconUrl
			}
		}

		transferSummary(assetName: $symbol, days: $days) {
			dailySummary {
				dateTime
				totalAmount
				transactionCount
			}
		}
	}
`

export const useStableCoinTokenTransferQuery = (
	symbol: string,
	days: number
) => {
	const { data, executeQuery, fetching } = useQuery({
		context: { url: useRuntimeConfig().public.apiUrlRust },
		query: STABLECOIN_TOKEN_TRANSFER,
		requestPolicy: 'cache-and-network',
		variables: { symbol, days },
	})

	return { data, executeQuery, fetching }
}
