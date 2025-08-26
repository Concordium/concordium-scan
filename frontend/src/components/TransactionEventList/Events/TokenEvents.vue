<template>
	<span v-if="event.event.__typename === 'TokenTransferEvent'">
		<span>
			Transferred
			<PltAmount
				:value="event.event.amount.value"
				:decimals="Number(event.event.amount.decimals)"
			/>
			<b>{{ event.tokenId }}</b>
			From

			<AccountLink :address="event.event.from.address.asString" />
		</span>
		<span>
			To
			<AccountLink :address="event.event.to.address.asString" />
		</span>
	</span>

	<span v-else-if="event.event.__typename === 'MintEvent'">
		<span>
			Minted
			<PltAmount
				:value="event.event.amount.value"
				:decimals="Number(event.event.amount.decimals)"
			/>
			<b>{{ event.tokenId }}</b>

			To
			<AccountLink :address="event.event.target.address.asString" />
		</span>
	</span>
	<span v-else-if="event.event.__typename === 'BurnEvent'">
		<span>
			Burned
			<PltAmount
				:value="event.event.amount.value"
				:decimals="Number(event.event.amount.decimals)"
			/>
			<b>{{ event.tokenId }}</b>

			From
			<AccountLink :address="event.event.target.address.asString" />
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
		<span
			v-if="
				event.event.eventType !== 'pause' && event.event.eventType !== 'unpause'
			"
		>
			Address:
			<AccountLink
				:address="event.event.details[event.event.eventType]?.target.address"
			/>
		</span>
	</span>
</template>

<script setup lang="ts">
import type { TokenUpdate } from '~/types/generated'
import AccountLink from '~/components/molecules/AccountLink.vue'

type Props = {
	event: TokenUpdate
}

defineProps<Props>()
</script>
