import { useQuery, gql } from '@urql/vue'
import { Ref } from 'vue'
import type { Account } from '~/types/generated'
import type { QueryVariables } from '~/types/queryVariables'
type AccountQueryVariables = {
	afterTx: QueryVariables['after']
	beforeTx: QueryVariables['before']
	firstTx: QueryVariables['first']
	lastTx: QueryVariables['last']
	afterReleaseSchedule: QueryVariables['after']
	beforeReleaseSchedule: QueryVariables['before']
	firstReleaseSchedule: QueryVariables['first']
	lastReleaseSchedule: QueryVariables['last']
}

const AccountQuery = gql<Account>`
	query (
		$id: ID!
		$afterTx: String
		$beforeTx: String
		$firstTx: Int
		$lastTx: Int
		$afterReleaseSchedule: String
		$beforeReleaseSchedule: String
		$firstReleaseSchedule: Int
		$lastReleaseSchedule: Int
	) {
		account(id: $id) {
			transactions(
				after: $afterTx
				before: $beforeTx
				first: $firstTx
				last: $lastTx
			) {
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
			transactionCount
			releaseSchedule {
				schedule(
					after: $afterReleaseSchedule
					before: $beforeReleaseSchedule
					first: $firstReleaseSchedule
					last: $lastReleaseSchedule
				) {
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
		$afterTx: String
		$beforeTx: String
		$firstTx: Int
		$lastTx: Int
		$afterReleaseSchedule: String
		$beforeReleaseSchedule: String
		$firstReleaseSchedule: Int
		$lastReleaseSchedule: Int
	) {
		accountByAddress(accountAddress: $address) {
			transactions(
				after: $afterTx
				before: $beforeTx
				first: $firstTx
				last: $lastTx
			) {
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
			transactionCount
			releaseSchedule {
				totalAmount
				schedule(
					after: $afterReleaseSchedule
					before: $beforeReleaseSchedule
					first: $firstReleaseSchedule
					last: $lastReleaseSchedule
				) {
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
	transactionVariables?: AccountQueryVariables
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
	transactionVariables?: AccountQueryVariables
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
