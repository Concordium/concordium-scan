import { useQuery, gql } from '@urql/vue'
import { Ref } from 'vue'
import type { Account } from '~/types/generated'
import type { QueryVariables } from '~/types/queryVariables'
const AccountQuery = gql<Account>`
	query ($id: ID!, $after: String, $before: String, $first: Int, $last: Int) {
		account(id: $id) {
			transactions(after: $after, before: $before, first: $first, last: $last) {
				pageInfo {
					startCursor
					endCursor
					hasPreviousPage
					hasNextPage
				}
				nodes {
					__typename
					transaction {
						id
						transactionHash
						senderAccountAddressString
						ccdCost
						result {
							__typename
						}
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
			}
			id
			addressString
			amount
			releaseSchedule {
				schedule {
					pageInfo {
						hasNextPage
						hasPreviousPage
						startCursor
						endCursor
					}
					nodes {
						transaction {
							transactionHash
						}
						timestamp
						amount
					}
				}
			}
			createdAt
			__typename
		}
	}
`

const AccountQueryByAddress = gql<Account>`
	query (
		$address: String!
		$after: String
		$before: String
		$first: Int
		$last: Int
	) {
		accountByAddress(accountAddress: $address) {
			transactions(after: $after, before: $before, first: $first, last: $last) {
				pageInfo {
					hasNextPage
					hasPreviousPage
					startCursor
					endCursor
					__typename
				}
				nodes {
					__typename
					transaction {
						id
						transactionHash
						senderAccountAddressString
						ccdCost
						block {
							blockSlotTime
						}
						result {
							__typename
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
			}
			id
			addressString
			amount
			releaseSchedule {
				totalAmount
				schedule {
					pageInfo {
						hasNextPage
						hasPreviousPage
						startCursor
						endCursor
					}
					nodes {
						transaction {
							transactionHash
							senderAccountAddressString
							ccdCost
							block {
								blockSlotTime
							}
							id
							transactionType {
								__typename
							}
							result {
								__typename
							}
						}
						timestamp
						amount
					}
				}
			}
			createdAt
			__typename
		}
	}
`

export const useAccountQuery = (
	id: Ref<string>,
	transactionVariables?: QueryVariables
) => {
	const { data } = useQuery({
		query: AccountQuery,
		requestPolicy: 'cache-first',
		variables: {
			id,
			...transactionVariables,
		},
	})

	return { data }
}
export const useAccountQueryByAddress = (
	address: Ref<string>,
	transactionVariables?: QueryVariables
) => {
	const { data } = useQuery({
		query: AccountQueryByAddress,
		requestPolicy: 'cache-first',
		variables: {
			address,
			...transactionVariables,
		},
	})

	return { data }
}
