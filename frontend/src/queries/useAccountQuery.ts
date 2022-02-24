import { useQuery, gql } from '@urql/vue'
import { Ref } from 'vue'
import type { Account } from '~/types/generated'
const AccountQuery = gql<Account>`
	query ($id: ID!) {
		account(id: $id) {
			transactions {
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
						senderAccountAddress
						ccdCost
						result {
							__typename
						}
					}
				}
			}
			id
			address
			createdAt
			__typename
		}
	}
`

const AccountQueryByAddress = gql<Account>`
	query ($address: String!) {
		accountByAddress(accountAddress: $address) {
			transactions {
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
						senderAccountAddress
						ccdCost
						result {
							__typename
						}
					}
				}
			}
			id
			address
			createdAt
			__typename
		}
	}
`

export const useAccountQuery = (id: Ref<string>) => {
	const { data } = useQuery({
		query: AccountQuery,
		requestPolicy: 'cache-first',
		variables: {
			id,
		},
	})

	return { data }
}
export const useAccountQueryByAddress = (address: Ref<string>) => {
	const { data } = useQuery({
		query: AccountQueryByAddress,
		requestPolicy: 'cache-first',
		variables: {
			address,
		},
	})

	return { data }
}
