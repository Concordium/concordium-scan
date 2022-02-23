import { useQuery, gql } from '@urql/vue'
import type { Account } from '~/types/generated'

const AccountQuery = gql<Account>`
	query ($id: ID!) {
		account(id: $id) {
			transactions {
				nodes {
					transaction {
						id
						transactionHash
						block {
							blockHash
							id
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
				nodes {
					transaction {
						id
						transactionHash
						block {
							blockHash
							id
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

export const useAccountQuery = (id: string) => {
	const { data } = useQuery({
		query: AccountQuery,
		requestPolicy: 'cache-first',
		variables: {
			id,
		},
	})

	return { data }
}
export const useAccountQueryByAddress = (address: string) => {
	const { data } = useQuery({
		query: AccountQueryByAddress,
		requestPolicy: 'cache-first',
		variables: {
			address,
		},
	})

	return { data }
}
