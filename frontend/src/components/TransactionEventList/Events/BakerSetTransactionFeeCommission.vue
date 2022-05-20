<template>
	<span>
		Baker <BakerLink :id="event.bakerId" /> set transaction fee commission to
		{{ convertMicroCcdToCcd(event.transactionFeeCommission) }}Ͼ</span
	>
</template>

<script setup lang="ts">
import BakerLink from '~/components/molecules/BakerLink.vue'
import type { BakerSetTransactionFeeCommission } from '~/types/generated'
import { convertMicroCcdToCcd } from '~/utils/format'

type Props = {
	event: BakerSetTransactionFeeCommission
}

defineProps<Props>()
</script>
