import { useQuery, gql } from '@urql/vue'
import { useComponentState } from '~/composables/useComponentState'
import type { Baker, InterimTransaction } from '~/types/generated'
import type { QueryVariables } from '~/types/queryVariables'

type BakerTxResponse = {
	bakerByBakerId: Baker
}

const BakerTxQuery = gql<BakerTxResponse>`
	query BakerTransactions(
		$bakerId: Long!
		$after: String
		$before: String
		$first: Int
		$last: Int
	) {
		bakerByBakerId(bakerId: $bakerId) {
			transactions(after: $after, before: $before, first: $first, last: $last) {
				nodes {
					transaction {
						id
						transactionHash
						block {
							blockSlotTime
						}
						transactionType {
							__typename
							... on AccountTransaction {
								accountTransactionType
							}
							... on CredentialDeploymentTransaction {
								credentialDeploymentTransactionType
							}
							... on UpdateTransaction {
								updateTransactionType
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
	}
`

export const useBakerTransactionsQuery = (
	bakerId: number,
	variables: Partial<QueryVariables>
) => {
	const { data, fetching, error } = useQuery({
		query: BakerTxQuery,
		requestPolicy: 'cache-and-network',
		variables: {
			bakerId,
			...variables,
		},
	})

	const dataRef = ref(data.value?.bakerByBakerId?.transactions?.nodes?.[0])

	const componentState = useComponentState<InterimTransaction | undefined>({
		fetching,
		error,
		data: dataRef,
	})

	watch(
		() => data.value,
		value => (dataRef.value = value?.bakerByBakerId?.transactions?.nodes?.[0])
	)

	return { data, error, componentState }
}
