import { useQuery, gql } from '@urql/vue'
import type { Transaction } from '~/types/transactions'

type TransactionResponse = {
	transaction: Transaction
}

const BlockQuery = gql<TransactionResponse>`
	query ($id: ID!) {
		transaction(id: $id) {
			ccdCost
			transactionHash
			senderAccountAddress
			block {
				blockHash
				blockHeight
				blockSlotTime
			}
			result {
				successful
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
`

export const useTransactionQuery = (id: string) => {
	const { data } = useQuery({
		query: BlockQuery,
		requestPolicy: 'cache-first',
		variables: {
			id,
		},
	})

	return { data }
}
