import { useSubscription, gql } from '@urql/vue'
import type { Subscription } from '~/types/generated'

type SubscriptionHandler<Subscription, R> = (
	previousData: R | undefined,
	data: Subscription
) => R

const SubscriptionQuery = gql<Subscription>`
	subscription accountsUpdatedSubscription($accountAddress: String!) {
		accountsUpdated(accountAddress: $accountAddress) {
			address
		}
	}
`
export const useAccountsUpdatedSubscription = (
	handleFunction: SubscriptionHandler<Subscription, void>,
	variables: { accountAddress: string }
) => {
	const { data, pause, resume } = useSubscription(
		{
			query: SubscriptionQuery,
			pause: true,
			variables,
		},
		handleFunction
	)

	return { data, pause, resume }
}
