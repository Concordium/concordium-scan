<template>
	<div>From:</div>
	<div>
		<AddressLink :address="event.fromAddress" />
	</div>
	<div>To:</div>
	<div>
		<AddressLink :address="event.toAddress" />
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
import { CisTransferEvent } from '../../../../src/types/generated'
import Log from '../../Details/Log.vue'
import TokenAmount from '../../atoms/TokenAmount.vue'
import AddressLink from '../../molecules/AddressLink.vue'

type Props = {
	event: CisTransferEvent
	decimals?: number | undefined
	symbol?: string | undefined
}
defineProps<Props>()
</script>
