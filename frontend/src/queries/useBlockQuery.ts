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
const BlockQuery = gql<BlockResponse>`
	query ($id: ID!, $after: String, $before: String, $first: Int, $last: Int) {
		block(id: $id) {
			id
			blockHash
			bakerId
			blockSlotTime
			finalized
			transactionCount
			transactions(after: $after, before: $before, first: $first, last: $last) {
				nodes {
					id
					transactionHash
					senderAccountAddress
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
			}
			specialEvents {
				mint {
					bakingReward
					finalizationReward
					foundationAccount
					platformDevelopmentCharge
				}
				finalizationRewards {
					remainder
					rewards {
						nodes {
							amount
							address
						}
					}
				}
				blockRewards {
					bakerReward
					transactionFees
					oldGasAccount
					newGasAccount
					foundationCharge
					bakerAccountAddress
					foundationAccountAddress
				}
			}
		}
	}
`

const BlockQueryByHash = gql<BlockByBlockHashResponse>`
	query (
		$hash: String!
		$after: String
		$before: String
		$first: Int
		$last: Int
	) {
		blockByBlockHash(blockHash: $hash) {
			id
			blockHash
			bakerId
			blockSlotTime
			finalized
			transactionCount
			transactions(after: $after, before: $before, first: $first, last: $last) {
				nodes {
					id
					transactionHash
					senderAccountAddress
					ccdCost
					result {
						__typename
					}
				}
				pageInfo {
					startCursor
					endCursor
					hasPreviousPage
					hasNextPage
				}
			}
			specialEvents {
				mint {
					bakingReward
					finalizationReward
					foundationAccount
					platformDevelopmentCharge
				}
				finalizationRewards {
					remainder
					rewards {
						nodes {
							amount
							address
						}
					}
				}
				blockRewards {
					bakerReward
					transactionFees
					oldGasAccount
					newGasAccount
					foundationCharge
					bakerAccountAddress
					foundationAccountAddress
				}
			}
		}
	}
`
export const useBlockQueryByHash = (
	hash: Ref<string>,
	eventsVariables?: QueryVariables
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
	eventsVariables?: QueryVariables
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
