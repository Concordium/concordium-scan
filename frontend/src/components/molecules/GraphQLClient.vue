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

const { apiUrl, wsUrl, includeDevTools } = useRuntimeConfig()

const subscriptionClient = new SubscriptionClient(wsUrl, { reconnect: true })

let exchanges = [
	...defaultExchanges,
	subscriptionExchange({
		forwardSubscription: operation => subscriptionClient.request(operation),
	}),
]

if (includeDevTools) {
	const dtools = await import('@urql/devtools')
	exchanges = [dtools.devtoolsExchange, ...exchanges]
}

const composeApiUrl = () =>
	location.host.includes('testnet')
		? apiUrl.replace('mainnet', 'testnet')
		: apiUrl

const client = createClient({
	url: composeApiUrl(),
	exchanges,
})

provideClient(client)
</script>
