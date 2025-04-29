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
</template>
<script lang="ts" setup>
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
