import { useQuery, gql } from '@urql/vue'
import type { Block } from '~/types/blocks'

type BlockResponse = {
	block: Block
}
type BlockByBlockHashResponse = {
	blockByBlockHash: Block
}
const BlockQuery = gql<BlockResponse>`
	query ($id: ID!) {
		block(id: $id) {
			id
			blockHash
			bakerId
			blockSlotTime
			finalized
			transactionCount
			transactions {
				nodes {
					id
					transactionHash
					senderAccountAddress
					ccdCost
					result {
						successful
					}
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
	query ($hash: String!) {
		blockByBlockHash(blockHash: $hash) {
			id
			blockHash
			bakerId
			blockSlotTime
			finalized
			transactionCount
			transactions {
				nodes {
					id
					transactionHash
					senderAccountAddress
					ccdCost
					result {
						successful
					}
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
export const useBlockQueryByHash = (hash: string) => {
	const { data } = useQuery({
		query: BlockQueryByHash,
		requestPolicy: 'cache-first',
		variables: {
			hash,
		},
	})
	return { data }
}

export const useBlockQuery = (id: string) => {
	const { data } = useQuery({
		query: BlockQuery,
		requestPolicy: 'cache-first',
		variables: {
			id,
		},
	})

	return { data }
}
