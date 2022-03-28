import { useSubscription, gql } from '@urql/vue'
import { type Subscription } from '~/types/generated'

type BlockSubscriptionHandler<Subscription, R> = (
	previousData: R | undefined,
	data: Subscription
) => R

const BlockSubscription = gql<Subscription>`
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
					senderAccountAddressString
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
	handleFunction: BlockSubscriptionHandler<Subscription, void>
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
