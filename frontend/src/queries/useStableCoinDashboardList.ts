import { useQuery, gql } from '@urql/vue'

export type Holder = {
	address?: string
	holdings?: Holding[]
	symbol?: string
	assetName?: string
	percentage?: number
	quantity?: number
}

export type Holding = {
	quantity?: number
	percentage?: number
}

export type Transaction = {
	transactionHash?: string
	dateTime?: string
	from?: string
	to?: string
	signature?: string
	date?: string
	amount?: number
	value?: number
}

export type StableCoin = {
	name?: string
	symbol?: string
	valueInDoller?: number
	totalUniqueHolder?: number
	totalSupply?: number
	circulatingSupply?: number
	holdings?: Holder[]
	transactions?: Transaction[]
}

export interface StableCoinDashboardOptions {
	limit?: number
	lastNTransactions?: number
}

export type StableCoinDashboardListResponse = {
	stablecoin: StableCoin
}

const STABLECOIN_DASHBOARD_LIST_QUERY = gql<StableCoinDashboardListResponse>`
	query SampleQuery($symbol: String!, $limit: Int!, $lastNTransactions: Int!) {
		stablecoin(
			symbol: $symbol
			limit: $limit
			lastNTransactions: $lastNTransactions
		) {
			name
			symbol
			totalSupply
			circulatingSupply
			metadata {
				iconUrl
			}
			transactions {
				from
				to
				assetName
				dateTime
				amount
				transactionHash
				value
			}
			transfers {
				assetName
				dateTime
				amount
			}
			holdings {
				address
				assetName
				quantity
				percentage
			}
		}
	}
`

export const useStableCoinDashboardList = (
	symbol: string,
	limit: Ref<number>,
	lastNTransactions: Ref<number>
) => {
	console.log(symbol, limit, lastNTransactions)
	const { data, executeQuery, fetching } = useQuery({
		context: { url: useRuntimeConfig().public.apiUrlRust },
		query: STABLECOIN_DASHBOARD_LIST_QUERY,
		requestPolicy: 'cache-and-network',
		variables: { symbol, limit, lastNTransactions },
	})

	return { data, executeQuery, fetching }
}
