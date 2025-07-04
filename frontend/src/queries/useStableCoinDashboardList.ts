import { useQuery, gql } from '@urql/vue'
import type { StableCoin } from '~/types/generated'

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
		query: STABLECOIN_DASHBOARD_LIST_QUERY,
		requestPolicy: 'cache-and-network',
		variables: { symbol, limit, lastNTransactions },
	})

	return { data, executeQuery, fetching }
}
