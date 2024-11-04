<template>
	<div>
		<slot />
	</div>
</template>

<script setup lang="ts">
import {
	createClient,
	defaultExchanges,
	subscriptionExchange,
	provideClient,
} from '@urql/vue'
import { SubscriptionClient } from 'subscriptions-transport-ws'

const {
	public: { apiUrl, wsUrl, enableUrqlDevtools },
} = useRuntimeConfig()

const subscriptionClient = new SubscriptionClient(wsUrl, {
	reconnect: true,
})

let exchanges = [
	...defaultExchanges,
	subscriptionExchange({
		forwardSubscription: operation => subscriptionClient.request(operation),
	}),
]

if (enableUrqlDevtools) {
	const dtools = await import('@urql/devtools')
	exchanges = [dtools.devtoolsExchange, ...exchanges]
}

const client = createClient({
	url: apiUrl,
	exchanges,
})

provideClient(client)
</script>
