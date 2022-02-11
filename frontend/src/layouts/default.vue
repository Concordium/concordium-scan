<template>
	<div>
		<Title>CCDScan</Title>
		<Link rel="icon" href="/favicon.svg" />

		<DrawerContainer />
		<div id="app" class="bg-theme-background-primary max-w-screen min-h-screen">
			<PageHeader />
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
import DrawerContainer from '~/components/Drawer/DrawerContainer.vue'
import { useDrawer } from '~/composables/useDrawer'

useMeta({
	meta: [{ link: [{ rel: 'icon', href: '/favicon.svg' }] }],
})

const { apiUrl, wsUrl, includeDevTools } = useRuntimeConfig()

let subscriptionClient: SubscriptionClient
if (process.client) {
	// We cannot run websockets serverside.
	subscriptionClient = new SubscriptionClient(wsUrl, { reconnect: true })
}
let exchanges = process.client
	? [
			...defaultExchanges,
			subscriptionExchange({
				forwardSubscription: operation => subscriptionClient.request(operation),
			}),
	  ]
	: defaultExchanges
if (includeDevTools) {
	const dtools = await import('@urql/devtools')
	exchanges = [dtools.devtoolsExchange, ...exchanges]
}
const client = createClient({
	url: apiUrl,
	exchanges,
})
const route = useRoute()
const {
	updateByRouteData: drawerupdateByRouteData,
	handleInitialLoad: drawerhandleInitialLoad,
} = useDrawer()

drawerhandleInitialLoad(route)
watch(route, to => {
	drawerupdateByRouteData(to)
})

provideClient(client)
</script>
