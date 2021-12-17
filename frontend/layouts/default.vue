<template>
	<div class="bg-theme-background-primary min-h-screen">
		<page-header />
		<slot />
	</div>
</template>

<script lang="ts">
import { defineComponent } from 'vue'
import { createClient, provideClient } from '@urql/vue'

const client = createClient({
	url: 'https://countries.trevorblades.com/',
})

export default defineComponent({
	setup() {
		provideClient(client)
	},
})
</script>
