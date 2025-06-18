<template>
	<span v-if="event.event.__typename === 'TokenTransferEvent'">
		<span>
			Transferred {{ event.event.amount.value }} <b>{{ event.tokenId }}</b>
			From

			<AccountLink :address="event.event.from.address" />
		</span>
		<span>
			To
			<AccountLink :address="event.event.to.address" />
		</span>
	</span>

	<span v-else-if="event.event.__typename === 'MintEvent'">
		<span>
			Minted {{ event.event.amount.value }} <b>{{ event.tokenId }}</b>

			To
			<AccountLink :address="event.event.target.address" />
		</span>
	</span>
	<span v-else-if="event.event.__typename === 'BurnEvent'">
		<span>
			Burned {{ event.event.amount.value }} <b>{{ event.tokenId }}</b>

			To
			<AccountLink :address="event.event.target.address" />
		</span>
	</span>
	<span v-else-if="event.event.__typename === 'TokenModuleEvent'">
		<span>
			Event Type:
			{{ event.event.eventType.replace(/^./, str => str.toUpperCase()) }}
		</span>
		<br />
		<span> Token Id: {{ event.tokenId }} </span>
		<br />
		<span>
			Address:
			<AccountLink
				:address="
					event.event.details[event.event.eventType].target.holderAccount
						.address
				"
			/>
		</span>
	</span>
</template>

<script setup lang="ts">
import type { TokenHolderEvent, TokenGovernanceEvent } from '~/types/generated'
import AccountLink from '~/components/molecules/AccountLink.vue'

type Props = {
	event: TokenHolderEvent | TokenGovernanceEvent
}

defineProps<Props>()
</script>
