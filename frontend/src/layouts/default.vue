<template>
	<div>
		<Title>CCDScan</Title>
		<Link rel="icon" href="/favicon.svg" />

		<BlockDetails />
		<TransactionDetails />

		<div id="app" class="bg-theme-background-primary min-h-screen">
			<page-header />
			<slot />
		</div>
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

useMeta({
	meta: [{ link: [{ rel: 'icon', href: '/favicon.svg' }] }],
})

const { apiUrl } = useRuntimeConfig()

let subscriptionClient: SubscriptionClient
if (process.client) {
	// We cannot run websockets serverside.
	subscriptionClient = new SubscriptionClient(
		'wss://dev.api-mainnet.ccdscan.io/graphql',
		{ reconnect: true }
	)
}
const client = createClient({
	url: apiUrl,
	exchanges: process.client
		? [
				...defaultExchanges,
				subscriptionExchange({
					forwardSubscription: operation =>
						subscriptionClient.request(operation),
				}),
		  ]
		: defaultExchanges,
})

provideClient(client)
</script>
