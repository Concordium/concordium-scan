<template>
	<span>
		<span class="text-theme">
			Issue protocol level token {{ event.createPlt.tokenId }}
		</span>
		<br />
		<span
			v-for="eventItem in event.events"
			:key="eventItem.__typename"
			class="text-theme"
		>
			<TokenEvents :event="eventItem" />
		</span>
	</span>
</template>

<script setup lang="ts">
import type { TokenCreationDetails } from '~/types/generated'
import TokenEvents from '~/components/TransactionEventList/Events/TokenEvents.vue'
type Props = {
	event: TokenCreationDetails
}

defineProps<Props>()
</script>
