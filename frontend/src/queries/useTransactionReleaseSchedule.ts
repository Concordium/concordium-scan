import { useQuery, gql } from '@urql/vue'
import { Ref } from 'vue'
import type { Transaction } from '~/types/generated'
import type { QueryVariables } from '~/types/queryVariables'
type TransactionResponse = {
	transaction: Transaction
}
const TransactionReleaseScheduleQuery = gql<TransactionResponse>`
	query (
		$hash: String!
		$after: String
		$before: String
		$first: Int
		$last: Int
	) {
		transactionByTransactionHash(transactionHash: $hash) {
			result {
				... on Success {
					events {
						nodes {
							... on TransferredWithSchedule {
								fromAccountAddressString
								toAccountAddressString
								totalAmount
								amountsSchedule(
									after: $after
									before: $before
									first: $first
									last: $last
								) {
									pageInfo {
										hasNextPage
										hasPreviousPage
										startCursor
										endCursor
									}
									nodes {
										timestamp
										amount
									}
								}
							}
						}
					}
				}
			}
		}
	}
`
export const useTransactionReleaseSchedule = (
	hash: Ref<string>,
	schedulePaging: QueryVariables
) => {
	const { data } = useQuery({
		query: TransactionReleaseScheduleQuery,
		requestPolicy: 'cache-first',
		variables: {
			hash,
			...schedulePaging,
		},
	})

	return { data }
}
