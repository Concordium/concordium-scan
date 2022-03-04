import { useSubscription, gql } from '@urql/vue'
import type { BlockSubscriptionResponse } from '~/types/blocks'

type BlockSubscriptionHandler<BlockSubscriptionResponse, R> = (
	previousData: R | undefined,
	data: BlockSubscriptionResponse
) => R

const BlockSubscription = gql<BlockSubscriptionResponse>`
	subscription blockAddedSubscription {
		blockAdded {
			transactionCount
			blockHash
			blockHeight
			bakerId
			id
			finalized
			specialEvents {
				blockRewards {
					bakerReward
				}
			}
			transactions {
				nodes {
					id
					ccdCost
					transactionHash
					senderAccountAddress
					block {
						blockHeight
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
	}
`
export const useBlockSubscription = (
	handleFunction: BlockSubscriptionHandler<BlockSubscriptionResponse, void>
) => {
	const { data, pause, resume } = useSubscription(
		{
			query: BlockSubscription,
			pause: true,
		},
		handleFunction
	)

	return { data, pause, resume }
}
