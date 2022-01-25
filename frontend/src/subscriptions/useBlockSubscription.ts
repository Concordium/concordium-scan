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
		}
	}
`
export const useBlockSubscription = (
	handleFunction: BlockSubscriptionHandler<BlockSubscriptionResponse, void>
) => {
	const { data } = useSubscription(
		{
			query: BlockSubscription,
		},
		handleFunction
	)

	return { data }
}
