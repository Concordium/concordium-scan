import { useQuery, gql } from '@urql/vue'

type StablecoinOverviewResponse = {
	stablecoinOverview: {
		totalMarketcap?: number
		noOfTxnLast24H?: number
		numberOfUniqueHolder?: number
		valuesTransferdLast24H?: number
	}
}

const STABLECOIN_OVERVIEW_QUERY = gql`
	query {
		stablecoinOverview {
			totalMarketcap
			noOfTxnLast24H
			numberOfUniqueHolder
			valuesTransferdLast24H
		}
	}
`

export const useStablecoinOverviewQuery = () => {
	const { data } = useQuery<StablecoinOverviewResponse>({
		context: { url: useRuntimeConfig().public.apiUrlRust },
		query: STABLECOIN_OVERVIEW_QUERY,
		requestPolicy: 'cache-and-network',
	})

	return { data }
}
