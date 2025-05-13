import { useQuery, gql } from '@urql/vue'
import { useComponentState } from '~/composables/useComponentState'
import type { Account, PageInfo } from '~/types/generated'
import type { QueryVariables } from '~/types/queryVariables'

type TopDelegatorsResponse = {
	accounts: {
		nodes: Account[]
		pageInfo: PageInfo
	}
}

const TopDelegatorsQuery = gql<TopDelegatorsResponse>`
	query ($after: String, $before: String, $first: Int, $last: Int) {
		accounts(
			after: $after
			before: $before
			first: $first
			last: $last
			sort: DELEGATED_STAKE_DESC
			filter: { isDelegator: true }
		) {
			nodes {
				id
				address {
					asString
				}
				amount
				delegation {
					stakedAmount
					restakeEarnings
					delegationTarget {
						... on BakerDelegationTarget {
							bakerId
						}
					}
				}
			}
			pageInfo {
				startCursor
				endCursor
				hasPreviousPage
				hasNextPage
			}
		}
	}
`

export const useTopDelegatorsQuery = (variables: Partial<QueryVariables>) => {
	const { data, error, fetching } = useQuery({
		query: TopDelegatorsQuery,
		requestPolicy: 'cache-and-network',
		variables,
	})

	const dataRef = ref(data.value?.accounts?.nodes?.[0])

	const componentState = useComponentState<Account | undefined>({
		fetching,
		error,
		data: dataRef,
	})

	watch(
		() => data.value,
		value => (dataRef.value = value?.accounts.nodes?.[0])
	)

	return { data, error, componentState }
}
