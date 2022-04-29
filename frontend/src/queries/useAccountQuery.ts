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
	afterAccountStatement: QueryVariables['after']
	beforeAccountStatement: QueryVariables['before']
	firstAccountStatement: QueryVariables['first']
	lastAccountStatement: QueryVariables['last']
}
const AccountQueryFragment = `
accountStatement(
				after: $afterAccountStatement
				before: $beforeAccountStatement
				first: $firstAccountStatement
				last: $lastAccountStatement
			) {
  pageInfo {
    hasNextPage
    hasPreviousPage
    startCursor
    endCursor
		__typename
  }
  nodes {
    reference {
      __typename
      ... on Block {
        blockHash
       }
			... on Transaction {
			 transactionHash
			}
    }
    timestamp
    entryType
    amount
    accountBalance
    __typename
  }
}
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
			senderAccountAddress {
				asString
			}
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
address {
	asString
}
amount
transactionCount
baker {
	bakerId
	state {
		...on ActiveBakerState {
			__typename
			stakedAmount
		}
		...on RemovedBakerState {
			__typename
			removedAt
		}
	}
}
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
				senderAccountAddress {
					asString
				}
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
`

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
		$afterAccountStatement: String
		$beforeAccountStatement: String
		$firstAccountStatement: Int
		$lastAccountStatement: Int
	) {
		account(id: $id) {
			${AccountQueryFragment}
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
		$afterAccountStatement: String
		$beforeAccountStatement: String
		$firstAccountStatement: Int
		$lastAccountStatement: Int
	) {
		accountByAddress(accountAddress: $address) {
			${AccountQueryFragment}
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
