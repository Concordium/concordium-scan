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
	}
	transferSummary?: {
		dailySummary?: DailySummaryItem[]
	}
}

// GraphQL Query
const STABLECOIN_TOKEN_TRANSFER = gql`
	query ($symbol: String!, $days: Int!) {
		stablecoin(symbol: $symbol) {
			name
			totalSupply
			totalUniqueHolder
			valueInDoller
			decimal
			symbol
		}
		transferSummary(assetName: $symbol, days: $days) {
			dailySummary {
				date
				totalAmount
				transactionCount
			}
		}
	}
`

// Function with typed parameters
export const useStableCoinTokenTransferQuery = (
	symbol: string,
	days: number
) => {
	const { data } = useQuery<StableCoinTokenTransferResponse>({
		context: { url: useRuntimeConfig().public.apiUrlRust },
		query: STABLECOIN_TOKEN_TRANSFER,
		requestPolicy: 'cache-and-network',
		variables: { symbol, days },
	})

	return { data }
}
