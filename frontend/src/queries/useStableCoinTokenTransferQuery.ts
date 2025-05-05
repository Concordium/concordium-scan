import { useQuery, gql } from '@urql/vue'

// Define DailySummaryItem
export type DailySummaryItem = {
	dateTime: string
	totalAmount: string
	transactionCount: number
}

// Define main response type
export type StableCoinTokenTransferResponse = {
	stablecoin: {
		name?: string
		totalSupply?: string
		totalUniqueHolders?: number
		valueInDollar?: number
		decimal?: number
		symbol?: string
		issuer?: string
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
	days: Ref<number>
) => {
	const { data, executeQuery, fetching } = useQuery({
		context: { url: useRuntimeConfig().public.apiUrlRust },
		query: STABLECOIN_TOKEN_TRANSFER,
		requestPolicy: 'cache-and-network',
		variables: { symbol, days },
	})

	return { data, executeQuery, fetching }
}
