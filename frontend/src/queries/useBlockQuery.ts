import { useQuery, gql } from '@urql/vue'
import { Ref } from 'vue'
import type { Block } from '~/types/generated'
import type { QueryVariables } from '~/types/queryVariables'

type BlockResponse = {
	block: Block
}
type BlockByBlockHashResponse = {
	blockByBlockHash: Block
}

type BlockQueryVariables = {
	afterTx: QueryVariables['after']
	beforeTx: QueryVariables['before']
	firstTx: QueryVariables['first']
	lastTx: QueryVariables['last']
	afterFinalizationRewards: QueryVariables['after']
	beforeFinalizationRewards: QueryVariables['before']
	firstFinalizationRewards: QueryVariables['first']
	lastFinalizationRewards: QueryVariables['last']
}

const transactionsFragment = `
nodes {
	id
	transactionHash
	senderAccountAddress {
		asString
	}
	ccdCost
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
pageInfo {
	startCursor
	endCursor
	hasPreviousPage
	hasNextPage
}
`

const BlockQuery = gql<BlockResponse>`
	query ($id: ID!, $afterTx: String, $beforeTx: String, $firstTx: Int, $lastTx: Int, $afterFinalizationRewards: String, $beforeFinalizationRewards: String, $firstFinalizationRewards: Int, $lastFinalizationRewards: Int) {
		block(id: $id) {
			id
			blockHash
			bakerId
			blockSlotTime
			finalized
			transactionCount
			transactions(after: $afterTx, before: $beforeTx, first: $firstTx, last: $lastTx) {
				${transactionsFragment}
			}
			specialEventsOld {
				mint {
					bakingReward
					finalizationReward
					foundationAccount
					platformDevelopmentCharge
				}
				finalizationRewards {
					remainder
					rewards(after: $afterFinalizationRewards, before: $beforeFinalizationRewards, first: $firstFinalizationRewards, last: $lastFinalizationRewards) {
						nodes {
							amount
							address {
								asString
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
				blockRewards {
					bakerReward
					transactionFees
					oldGasAccount
					newGasAccount
					foundationCharge
					bakerAccountAddress {
						asString
					}
					foundationAccountAddress {
						asString
					}
				}
			}
		}
	}
`

const BlockQueryByHash = gql<BlockByBlockHashResponse>`
	query (
		$hash: String!
		$afterTx: String
		$beforeTx: String
		$firstTx: Int
		$lastTx: Int
		$afterFinalizationRewards: String
		$beforeFinalizationRewards: String
		$firstFinalizationRewards: Int
		$lastFinalizationRewards: Int
	) {
		blockByBlockHash(blockHash: $hash) {
			id
			blockHash
			bakerId
			blockSlotTime
			finalized
			transactionCount
			transactions(after: $afterTx, before: $beforeTx, first: $firstTx, last: $lastTx) {
				${transactionsFragment}
			}
			specialEventsOld {
				mint {
					bakingReward
					finalizationReward
					foundationAccount
					platformDevelopmentCharge
				}
				finalizationRewards {
					remainder
					rewards(after: $afterFinalizationRewards, before: $beforeFinalizationRewards, first: $firstFinalizationRewards, last: $lastFinalizationRewards) {
						nodes {
							amount
							address {
								asString
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
				blockRewards {
					bakerReward
					transactionFees
					oldGasAccount
					newGasAccount
					foundationCharge
					bakerAccountAddress {
						asString
					}
					foundationAccountAddress {
						asString
					}
				}
			}
		}
	}
`
export const useBlockQueryByHash = (
	hash: Ref<string>,
	eventsVariables?: BlockQueryVariables
) => {
	const { data } = useQuery({
		query: BlockQueryByHash,
		requestPolicy: 'cache-first',
		variables: {
			hash,
			...eventsVariables,
		},
	})
	return { data }
}

export const useBlockQuery = (
	id: Ref<string>,
	eventsVariables?: BlockQueryVariables
) => {
	const { data } = useQuery({
		query: BlockQuery,
		requestPolicy: 'cache-first',
		variables: {
			id,
			...eventsVariables,
		},
	})

	return { data }
}
