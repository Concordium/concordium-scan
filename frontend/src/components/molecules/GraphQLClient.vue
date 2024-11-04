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
import { composeBackendUrls } from '~/utils/composeBackendUrls'

const {
	public: { apiUrl, wsUrl, includeDevTools },
} = useRuntimeConfig()

const [composedApiUrl, composedWsUrl] = composeBackendUrls(
	apiUrl,
	wsUrl
)(location.host)

const subscriptionClient = new SubscriptionClient(composedWsUrl, {
	reconnect: true,
})

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

const client = createClient({
	url: composedApiUrl,
	exchanges,
})

provideClient(client)
</script>
