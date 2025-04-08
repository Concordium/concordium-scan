import { useQuery, gql } from '@urql/vue'

export type Stablecoin = {
	totalSupply?: number
	circulatingSupply?: number
	decimal?: number
	name?: string
	symbol?: string
	valueInDoller?: number
	totalUniqueHolder?: number
	supplyPercentage?: string
	percentage?: string
	address?: string
}

export type StablecoinResponse = {
	stablecoins: Stablecoin[]
	address?: string
	percentage?: number
}

const STABLECOIN_QUERY = gql`
	query {
		stablecoins {
			totalSupply
			circulatingSupply
			decimal
			name
			symbol
			valueInDoller
			totalUniqueHolder
		}
	}
`

export const useStableCoinsQuery = () => {
	const { data } = useQuery<StablecoinResponse>({
		context: { url: useRuntimeConfig().public.apiUrlRust },
		query: STABLECOIN_QUERY,
		requestPolicy: 'cache-and-network',
	})

	return { data }
}
