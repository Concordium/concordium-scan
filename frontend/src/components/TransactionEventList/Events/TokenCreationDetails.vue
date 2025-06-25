<template>
	<span>
		<span class="text-theme">
			Issue protocol level token {{ event.createPlt.tokenId }} with governance
			account
			<AccountLink :address="event.createPlt.governanceAccount.asString" />
		</span>
		<br />
		<span v-for="eventItem in event.events" :key="eventItem.__typename">
			<TokenEvents :event="eventItem" />
		</span>
	</span>
</template>

<script setup lang="ts">
import type { TokenCreationDetails } from '~/types/generated'
import AccountLink from '~/components/molecules/AccountLink.vue'
type Props = {
	event: TokenCreationDetails
}

defineProps<Props>()
</script>
