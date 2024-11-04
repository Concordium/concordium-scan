<template>
	<div>From:</div>
	<div>
		<AddressLink :address="event.fromAddress" />
	</div>
	<div>Amount:</div>
	<div>
		<TokenAmount
			:amount="event.tokenAmount"
			:symbol="symbol"
			:fraction-digits="Number(decimals || 0)"
		/>
	</div>
	<Log v-if="event.parsed" :log="event.parsed" />
</template>
<script lang="ts" setup>
import Log from '../../Details/Log.vue'
import type { CisBurnEvent } from '~/types/generated'
import TokenAmount from '~/components/atoms/TokenAmount.vue'
import AddressLink from '~/components/molecules/AddressLink.vue'

type Props = {
	event: CisBurnEvent
	decimals?: number | undefined
	symbol?: string | undefined
}
defineProps<Props>()
</script>
