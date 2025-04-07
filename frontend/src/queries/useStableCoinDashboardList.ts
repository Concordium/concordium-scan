import { useQuery, gql } from '@urql/vue'

export type Holder = {
	address?: string
	holdings?: Holding[]
	symbol?: string
}

export type Holding = {
	quantity?: number
	percentage?: number
}

export type StableCoin = {
	name?: string
	symbol?: string
	valueInDoller?: number
	totalUniqueHolder?: number
	totalSupply?: number
	circulatingSupply?: number
	holding?: Holder[]
}

export type StableCoinDashboardListResponse = {
	stablecoin: StableCoin
}

const STABLECOIN_DASHBOARD_LIST_QUERY = gql`
	query SampleQuery($symbol: String!, $topHolder: Int!) {
		stablecoin(symbol: $symbol, topHolder: $topHolder) {
			name
			symbol
			valueInDoller
			totalUniqueHolder
			totalSupply
			circulatingSupply
			holding {
				address
				holdings {
					quantity
					percentage
					# currency
				}
			}
		}
	}
`

export const useStableCoinDashboardList = ({
	symbol,
	topHolder,
}: {
	symbol: string
	topHolder: number
}) => {
	const { data } = useQuery<StableCoinDashboardListResponse>({
		context: { url: useRuntimeConfig().public.apiUrlRust },
		query: STABLECOIN_DASHBOARD_LIST_QUERY,
		requestPolicy: 'cache-and-network',
		variables: { symbol, topHolder },
	})

	return { data }
}
