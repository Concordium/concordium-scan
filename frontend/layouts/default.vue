<template>
	<div class="bg-theme-background-primary min-h-screen">
		<page-header />
		<slot />
	</div>
</template>

<script setup lang="ts">
import { createClient, provideClient } from '@urql/vue'

const { apiUrl } = useRuntimeConfig()

const client = createClient({
	url: apiUrl,
})

provideClient(client)
</script>
