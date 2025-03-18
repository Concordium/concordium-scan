import { useQuery, gql } from '@urql/vue'

const StableCoinsQuery = gql`
	query {
		stablecoins {
			name
			symbol
			decimal
			totalSupply
			transfers {
				from
				to
				amount
				date
			}
		}
	}
`
const stableCoinQuery = gql`
	query ($symbol: String!) {
		stablecoin(symbol: $symbol) {
			name
			symbol
			decimal
			totalSupply
			transfers {
				from
				to
				amount
				date
			}
		}
	}
`

export const useStableCoinsQuery = () => {
	const { data, executeQuery, fetching } = useQuery({
		context: { url: useRuntimeConfig().public.apiUrlRust },
		query: StableCoinsQuery,
		requestPolicy: 'cache-and-network',
	})

	return { data, executeQuery, fetching }
}

export const useStableCoinQuery = (symbol: string) => {
	const { data, executeQuery, fetching } = useQuery({
		context: { url: useRuntimeConfig().public.apiUrlRust },
		query: stableCoinQuery,
		requestPolicy: 'cache-and-network',
		variables: { symbol },
	})

	return { data, executeQuery, fetching }
}
