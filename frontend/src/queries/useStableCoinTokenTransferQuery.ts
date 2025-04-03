import { useQuery, gql } from '@urql/vue'

type StableCoinTokenTransferResponse = {
	stablecoin: {
		name?: string
	}
}

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

export const useStableCoinTokenTransferQuery = (symbol, days) => {
	const { data } = useQuery<StableCoinTokenTransferResponse>({
		context: { url: useRuntimeConfig().public.apiUrlRust },
		query: STABLECOIN_TOKEN_TRANSFER,
		requestPolicy: 'cache-and-network',
		variables: { symbol, days },
	})

	return { data }
}
