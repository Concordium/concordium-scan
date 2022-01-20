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
import { createClient, provideClient } from '@urql/vue'

useMeta({
	meta: [{ link: [{ rel: 'icon', href: '/favicon.svg' }] }],
})

const { apiUrl } = useRuntimeConfig()

const client = createClient({
	url: apiUrl,
})

provideClient(client)
</script>
