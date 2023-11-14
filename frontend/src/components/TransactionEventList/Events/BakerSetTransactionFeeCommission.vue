<template>
	<span>
		Validator <BakerLink :id="event.bakerId" /> set transaction fee commission
		to {{ event.transactionFeeCommission * 100 }}%</span
	>
</template>

<script setup lang="ts">
import BakerLink from '~/components/molecules/BakerLink.vue'
import type { BakerSetTransactionFeeCommission } from '~/types/generated'

type Props = {
	event: BakerSetTransactionFeeCommission
}

defineProps<Props>()
</script>
